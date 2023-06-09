﻿// <auto-generated />
using System;
using EidolonicBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EidolonicBot.Migrations
{
    [DbContext(typeof(PostgresDbContext))]
    [Migration("20230511055829_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:CollationDefinition:case_insensitive", "en-u-ks-primary,en-u-ks-primary,icu,False")
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("EidolonicBot.Models.Subscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text")
                        .UseCollation("case_insensitive");

                    b.HasKey("Id");

                    b.HasIndex("Address")
                        .IsUnique();

                    b.ToTable("Subscription");
                });

            modelBuilder.Entity("EidolonicBot.Models.SubscriptionByChat", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("SubscriptionId")
                        .HasColumnType("uuid");

                    b.HasKey("ChatId", "SubscriptionId");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("SubscriptionByChat");
                });

            modelBuilder.Entity("EidolonicBot.Models.SubscriptionByChat", b =>
                {
                    b.HasOne("EidolonicBot.Models.Subscription", "Subscription")
                        .WithMany("SubscriptionByChat")
                        .HasForeignKey("SubscriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subscription");
                });

            modelBuilder.Entity("EidolonicBot.Models.Subscription", b =>
                {
                    b.Navigation("SubscriptionByChat");
                });
#pragma warning restore 612, 618
        }
    }
}
