using System.Data;
using DbUp.Engine;

namespace infrastructure.Database.Migrations;

public class Script_0001_CreateListsTable : IScript 
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
	    const string createListsTable = """
	                                    CREATE TABLE list (
	                                        id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	                                        user_id VARCHAR(36) NOT NULL,
	                                        name VARCHAR(64) NOT NULL,
	                                        description VARCHAR(256) NULL DEFAULT NULL,
	                                        url VARCHAR(22) NOT NULL UNIQUE,
	                                        currency VARCHAR(5) NOT NULL,
	                                        public BOOLEAN NOT NULL,
	                                        deleted BOOLEAN NOT NULL,
	                                        updated_at BIGINT NOT NULL,
	                                        created_at BIGINT NOT NULL
	                                    );
	                                    """;
	    
	    return createListsTable;
    }
}