﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using MountainWarehouse.EasyMWS.Data;
using MountainWarehouse.EasyMWS.Enums;
using System;

namespace MountainWarehouse.EasyMWS.Migrations
{
    [DbContext(typeof(EasyMwsContext))]
    partial class EasyMwsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.AmazonReport", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Content");

                    b.Property<DateTime>("DateCreated");

                    b.Property<string>("DownloadRequestId");

                    b.Property<string>("DownloadTimestamp");

                    b.Property<string>("ReportType");

                    b.HasKey("Id");

                    b.ToTable("AmazonReports");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.FeedSubmissionCallback", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AmazonRegion");

                    b.Property<string>("Data");

                    b.Property<string>("DataTypeName");

                    b.Property<string>("FeedSubmissionData");

                    b.Property<string>("FeedSubmissionId");

                    b.Property<string>("FeedType");

                    b.Property<bool>("HasErrors");

                    b.Property<bool>("IsProcessingComplete");

                    b.Property<DateTime>("LastSubmitted");

                    b.Property<string>("MerchantId");

                    b.Property<string>("MethodName");

                    b.Property<string>("SubmissionErrorData");

                    b.Property<int>("SubmissionRetryCount");

                    b.Property<string>("TypeName");

                    b.HasKey("Id");

                    b.HasIndex("FeedSubmissionId");

                    b.ToTable("FeedSubmissionCallbacks");
                });

            modelBuilder.Entity("MountainWarehouse.EasyMWS.Data.ReportRequestCallback", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AmazonRegion");

                    b.Property<int>("ContentUpdateFrequency");

                    b.Property<string>("Data");

                    b.Property<string>("DataTypeName");

                    b.Property<string>("GeneratedReportId");

                    b.Property<DateTime>("LastRequested");

                    b.Property<string>("MerchantId");

                    b.Property<string>("MethodName");

                    b.Property<string>("ReportRequestData");

                    b.Property<string>("ReportType");

                    b.Property<string>("RequestReportId");

                    b.Property<int>("RequestRetryCount");

                    b.Property<string>("TypeName");

                    b.HasKey("Id");

                    b.HasIndex("RequestReportId", "GeneratedReportId");

                    b.ToTable("ReportRequestCallbacks");
                });
#pragma warning restore 612, 618
        }
    }
}
