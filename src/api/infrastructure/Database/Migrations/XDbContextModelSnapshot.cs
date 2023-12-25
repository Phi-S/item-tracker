﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using infrastructure.Database;

#nullable disable

namespace infrastructure.Database.Migrations
{
    [DbContext(typeof(XDbContext))]
    partial class XDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("infrastructure.Database.Models.ItemListDbModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.Property<bool>("Deleted")
                        .HasColumnType("boolean");

                    b.Property<string>("Description")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<bool>("Public")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatedUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(22)
                        .HasColumnType("character varying(22)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasMaxLength(36)
                        .HasColumnType("character varying(36)");

                    b.HasKey("Id");

                    b.HasIndex("Url")
                        .IsUnique();

                    b.ToTable("Lists");
                });

            modelBuilder.Entity("infrastructure.Database.Models.ItemListItemActionDbModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(1)
                        .HasColumnType("character varying(1)");

                    b.Property<int>("Amount")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("ItemId")
                        .HasColumnType("bigint");

                    b.Property<long>("ListId")
                        .HasColumnType("bigint");

                    b.Property<long>("UnitPrice")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ListId");

                    b.ToTable("ItemActions");
                });

            modelBuilder.Entity("infrastructure.Database.Models.ItemPriceDbModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<long?>("Buff163PriceCentsUsd")
                        .HasColumnType("bigint");

                    b.Property<long>("ItemId")
                        .HasColumnType("bigint");

                    b.Property<long>("ItemPriceRefreshId")
                        .HasColumnType("bigint");

                    b.Property<long?>("SteamPriceCentsUsd")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("ItemPriceRefreshId");

                    b.ToTable("Prices");
                });

            modelBuilder.Entity("infrastructure.Database.Models.ItemPriceRefreshDbModel", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("Buff163PricesLastModified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedUtc")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("SteamPricesLastModified")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double>("UsdToEurExchangeRate")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.ToTable("PricesRefresh");
                });

            modelBuilder.Entity("infrastructure.Database.Models.ItemListItemActionDbModel", b =>
                {
                    b.HasOne("infrastructure.Database.Models.ItemListDbModel", "List")
                        .WithMany()
                        .HasForeignKey("ListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("List");
                });

            modelBuilder.Entity("infrastructure.Database.Models.ItemPriceDbModel", b =>
                {
                    b.HasOne("infrastructure.Database.Models.ItemPriceRefreshDbModel", "ItemPriceRefresh")
                        .WithMany()
                        .HasForeignKey("ItemPriceRefreshId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ItemPriceRefresh");
                });
#pragma warning restore 612, 618
        }
    }
}
