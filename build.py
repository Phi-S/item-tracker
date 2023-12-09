import os
import subprocess
import sys
import re

working_directory = os.path.dirname(os.path.realpath(__file__))


#  python3 build.py -BUILD_VERSION 1.0.0 -PUBLISH -DOCKER_REGISTRY docker-registry.theaurum.net -DOCKER_REGISTRY_USERNAME username -DOCKER_REGISTRY_PASSWORD password
def main():
    print(f"{working_directory=}")
    docker_image_name = "item-tracker"
    docker_image_name_api = f"{docker_image_name}-api"
    docker_image_dockerfile_api = "Dockerfile_api"
    docker_image_name_web = f"{docker_image_name}-web"
    docker_image_dockerfile_web = "Dockerfile_web"

    args: list[str] = sys.argv[1:]
    build_version = get_arg_value(args, "-BUILD_VERSION")
    if not re.search("^\d{0,3}.\d{0,3}.\d{0,3}$", build_version):
        raise Exception(
            f"{build_version} is not valid."
            f" Valid versions examples: \"123.123.123\", \"1.2.3\", \"1.2.123\"")

    docker_image_name_with_version_instance = f"{docker_image_name_api}:{build_version}"
    print(f"Building {docker_image_name_with_version_instance} docker image")
    build(docker_image_name_with_version_instance, docker_image_dockerfile_api)

    docker_image_name_with_version_web = f"{docker_image_name_web}:{build_version}"
    print(f"Building {docker_image_name_with_version_web} docker image")
    build(docker_image_name_with_version_web, docker_image_dockerfile_web)

    should_publish = "-PUBLISH" in args
    if not should_publish:
        return

    git_status = run_command(["git", "status", "--porcelain"])
    if git_status:
        raise Exception("Git repo got pending changes")

    docker_registry = get_arg_value(args, "-DOCKER_REGISTRY")
    docker_registry_username = get_arg_value(args, "-DOCKER_REGISTRY_USERNAME")
    docker_registry_password = get_arg_value(args, "-DOCKER_REGISTRY_PASSWORD")

    print("Publishing...")
    print(f"{docker_registry}=")
    docker_image_name_for_registry_with_version_instance = publish_docker(
        docker_image_name_api,
        build_version,
        docker_registry,
        docker_registry_username,
        docker_registry_password)
    print(f"Docker image {docker_image_name_for_registry_with_version_instance} published")

    docker_image_name_for_registry_with_version_web = publish_docker(
        docker_image_name_web,
        build_version,
        docker_registry,
        docker_registry_username,
        docker_registry_password)
    print(f"Docker image {docker_image_name_for_registry_with_version_web} published")

    publish_git(
        build_version,
        docker_image_name_for_registry_with_version_instance,
        docker_image_name_for_registry_with_version_web)
    print(f"git {build_version} tag pushed")
    print("DONE DONE DONE")


################################################
def run_command(command: list[str]):
    command_as_string = " ".join(command)
    print(f"Executing command \"{command_as_string}\"")

    p = subprocess.Popen(
        command,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        universal_newlines=True,
        cwd=working_directory)

    out = ""

    while p.poll() is None:
        line = p.stdout.readline()  # This blocks until it receives a newline.
        if not line:
            continue
        print(line.rstrip())
        out += line

    p.communicate()

    if p.returncode != 0:
        raise Exception(f"Error while trying to execute command: \"{command_as_string}\"")

    return out


def get_arg_value(args_parameter: list[str], arg_to_find: str) -> str:
    for i in range(len(args_parameter)):
        arg = args_parameter[i]
        if arg.lower() == arg_to_find.lower():
            build_version_result = args_parameter[i + 1]
            return build_version_result


def build(docker_image_name_with_version: str, docker_file_path: str):
    run_command(
        ["docker", "build",
         "-t", docker_image_name_with_version,
         "--force-rm",
         "-f", docker_file_path,
         "."]
    )


def publish_docker(docker_image_name: str,
                   build_version: str,
                   docker_registry: str,
                   docker_registry_username: str,
                   docker_registry_password: str) -> str:
    run_command(["docker", "logout"])
    run_command(["docker", "login",
                 "--username", docker_registry_username,
                 "--password", docker_registry_password,
                 docker_registry])
    docker_image_name_with_version = f"{docker_image_name}:{build_version}"
    docker_image_name_for_registry_with_version = f"{docker_registry}/{docker_image_name}:{build_version}"
    docker_image_name_for_registry_latest = f"{docker_registry}/{docker_image_name}:latest"
    run_command(["docker", "tag", docker_image_name_with_version, docker_image_name_for_registry_with_version])
    run_command(["docker", "tag", docker_image_name_with_version, docker_image_name_for_registry_latest])
    run_command(["docker", "image", "push", docker_image_name_for_registry_with_version])
    run_command(["docker", "image", "push", docker_image_name_for_registry_latest])
    return docker_image_name_for_registry_with_version


def git_switching_to_main_branch(git_main_branch: str):
    run_command(["git", "fetch", "origin", "-v"])
    run_command(["git", "switch", git_main_branch])
    run_command(["git", "reset", "--hard", "HEAD"])
    run_command(["git", "pull"])
    run_command(["git", "clean", "-d", "-f"])


def publish_git(build_version: str, docker_image_with_version_instance: str, docker_image_with_version_web: str):
    run_command(["git", "tag", "-a", f"{build_version}", "-m",
                 f"{build_version}\n"
                 f"{docker_image_with_version_instance}\n"
                 f"{docker_image_with_version_web}\n"])
    run_command(["git", "push", "--tags"])


if __name__ == "__main__":
    main()
