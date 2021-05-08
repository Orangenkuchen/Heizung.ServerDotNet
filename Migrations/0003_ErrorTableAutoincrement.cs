namespace Heizung.ServerDotNet.Migrations
{
    using FluentMigrator;

    /// <summary>
    /// Fügt der Tabelle ErrorList ein Autoincrement auf Id hinzu
    /// </summary>
    [Migration(3, "Fügt der Tabelle ErrorList ein Autoincrement auf Id hinzu.")]
    public class _0003_ErrorTableAutoincrement : Migration
    {
        #region Up
        /// <summary>
        /// Migriert zur nächsten Version
        /// </summary>
        public override void Up()
        {
            base.Execute.Sql("ALTER TABLE Heizung.ErrorList MODIFY COLUMN Id int(10) unsigned auto_increment NOT NULL COMMENT 'Die Id vom Error.';");
        }
        #endregion

        #region Down
        /// <summary>
        /// Migriert zur vorherigen Version
        /// </summary>
        public override void Down()
        {
            base.Execute.Script("ALTER TABLE Heizung.ErrorList MODIFY COLUMN Id int(10) unsigned NOT NULL COMMENT 'Die Id vom Error.';");
        }
        #endregion
    }
}

//ALTER TABLE Heizung.ErrorList MODIFY COLUMN Id int(10) unsigned auto_increment NOT NULL COMMENT 'Die Id vom Error.';
