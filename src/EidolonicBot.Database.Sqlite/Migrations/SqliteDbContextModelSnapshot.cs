﻿// <auto-generated />
using System;
using EidolonicBot;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EidolonicBot.Migrations
{
    [DbContext(typeof(SqliteDbContext))]
    partial class SqliteDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("EidolonicBot.Models.Subscription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .UseCollation("NOCASE");

                    b.HasKey("Id");

                    b.HasIndex("Address")
                        .IsUnique();

                    b.ToTable("Subscription");
                });

            modelBuilder.Entity("EidolonicBot.Models.SubscriptionByChat", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageThreadId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("SubscriptionId")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("MinDelta")
                        .HasColumnType("TEXT");

                    b.HasKey("ChatId", "MessageThreadId", "SubscriptionId");

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
