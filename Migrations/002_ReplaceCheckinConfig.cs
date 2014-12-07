using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 2, "1.0.12" )]
    public class ChangeAreaConfig : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateBlockType( "Area Configuration", "Attended Check-In Area Config", "~/Plugins/cc_newspring/AttendedCheckIn/AreaConfiguration.ascx", "Check-in > Attended", "FADD6974-FE07-49EF-AA8D-5AE5976D85D2" );
            Sql( @"
                DECLARE @NewConfigBlockTypeId int = (SELECT [ID] FROM [BlockType] WHERE [Guid] = 'FADD6974-FE07-49EF-AA8D-5AE5976D85D2')
                DECLARE @OldConfigBlockTypeId int = (SELECT [ID] FROM [BlockType] WHERE [Guid] = '2506B048-F62C-4945-B09A-1E053F66C592')
                UPDATE [Block] SET [BlockTypeId] = @NewConfigBlockTypeId WHERE [BlockTypeId] = @OldConfigBlockTypeId AND [Name] = 'Check-in Configuration'
            " );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            Sql( @"
                DECLARE @NewConfigBlockTypeId int = (SELECT [ID] FROM [BlockType] WHERE [Guid] = 'FADD6974-FE07-49EF-AA8D-5AE5976D85D2')
                DECLARE @OldConfigBlockTypeId int = (SELECT [ID] FROM [BlockType] WHERE [Guid] = '2506B048-F62C-4945-B09A-1E053F66C592')
                UPDATE [Block] SET [BlockTypeId] = @OldConfigBlockTypeId WHERE [BlockTypeId] = @NewConfigBlockTypeId AND [Name] = 'Check-in Configuration'
            " );
            RockMigrationHelper.DeleteBlockType( "FADD6974-FE07-49EF-AA8D-5AE5976D85D2" );
        }
    }
}