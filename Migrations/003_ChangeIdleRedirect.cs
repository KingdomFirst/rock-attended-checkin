using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 3, "1.0.14" )]
    public class ChangeIdleRedirect : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            // Confirmation
            RockMigrationHelper.AddBlock( "BE996C9B-3DFE-407F-BD53-D6F58D85A035", "", "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "Idle Redirect", "Main", "", "", 1, "FAEC5FCC-B850-4DA6-8844-715159D39BD5" );

            RockMigrationHelper.AddBlockTypeAttribute( "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "9C204CD0-1233-41C5-818A-C5DA439445AA", "New Location", "NewLocation", "~/attendedcheckin/search", "The new location URL to send user to after idle time", 0, @"", "C1D6355B-2FF5-4FBB-AE91-1F76B43E4DB4" );

            RockMigrationHelper.AddBlockTypeAttribute( "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Idle Seconds", "IdleSeconds", "", "How many seconds of idle time to wait before redirecting user", 0, @"300", "BB5EF303-E4B5-43F9-978C-DE7B3946CA7D" );

            RockMigrationHelper.AddBlockAttributeValue( "FAEC5FCC-B850-4DA6-8844-715159D39BD5", "C1D6355B-2FF5-4FBB-AE91-1F76B43E4DB4", @"~/attendedcheckin/search" ); // New Location
            RockMigrationHelper.AddBlockAttributeValue( "FAEC5FCC-B850-4DA6-8844-715159D39BD5", "BB5EF303-E4B5-43F9-978C-DE7B3946CA7D", @"300" ); // Idle Seconds

            // Activity Select
            RockMigrationHelper.AddBlock( "C87916FE-417E-4A11-8831-5CFA7678A228", "", "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "Idle Redirect", "Main", "", "", 1, "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203" );

            RockMigrationHelper.AddBlockTypeAttribute( "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "9C204CD0-1233-41C5-818A-C5DA439445AA", "New Location", "NewLocation", "~/attendedcheckin/search", "The new location URL to send user to after idle time", 0, @"", "02FB40A5-747B-441C-9825-2CA314DE6767" );

            RockMigrationHelper.AddBlockTypeAttribute( "0DF27F26-691D-41F8-B0F7-987E4FEC375C", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Idle Seconds", "IdleSeconds", "", "How many seconds of idle time to wait before redirecting user", 0, @"300", "AD90C10D-27B7-47C1-B07B-8074E94E4A5F" );

            RockMigrationHelper.AddBlockAttributeValue( "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203", "02FB40A5-747B-441C-9825-2CA314DE6767", @"~/attendedcheckin/search" ); // New Location
            RockMigrationHelper.AddBlockAttributeValue( "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203", "AD90C10D-27B7-47C1-B07B-8074E94E4A5F", @"300" ); // Idle Seconds
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "C1D6355B-2FF5-4FBB-AE91-1F76B43E4DB4" );
            RockMigrationHelper.DeleteAttribute( "BB5EF303-E4B5-43F9-978C-DE7B3946CA7D" );
            RockMigrationHelper.DeleteBlock( "FAEC5FCC-B850-4DA6-8844-715159D39BD5" );
        }
    }
}
