using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Newsblast.Shared.Migrations
{
    public partial class RemoveFeedGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFeedGuid",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "FeedGuid",
                table: "Embeds");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDate",
                table: "Subscriptions",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDate",
                table: "Subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "LastFeedGuid",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedGuid",
                table: "Embeds",
                nullable: false,
                defaultValue: "");
        }
    }
}
