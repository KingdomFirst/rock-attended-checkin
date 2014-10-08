using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 2, "1.0.14" )]
    public class AddCheckInNoteType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Sql( @"
                DECLARE @PersonEntityTypeId int = (SELECT [ID] FROM [EntityType] WHERE [Guid] = '72657ED8-D16E-492E-AC12-144C5E7567E7')
                INSERT [NoteType] (IsSystem, EntityTypeId, Name, Guid)
                SELECT 0, @PersonEntityTypeId, 'Check-In', '2BBA0589-6EC2-47F6-8745-34E95E3AC019'
            " );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            Sql( @"
                DELETE FROM [NoteType] WHERE [Guid] = '2BBA0589-6EC2-47F6-8745-34E95E3AC019'
            " );
        }
    }
}
