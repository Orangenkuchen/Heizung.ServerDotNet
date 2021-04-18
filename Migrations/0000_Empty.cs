namespace Heizung.ServerDotNet.Migrations
{
    using FluentMigrator;
    
    /// <summary>
    /// Migration 0. Diese kann dazu verwendet werden, die Datenbank zu löschen
    /// </summary>
    [Migration(0)]
    public class _0000_Empty : Migration
    {
        #region Up
        /// <summary>
        /// Migriert zur nächsten Version
        /// </summary>
        public override void Up()
        {
        }
        #endregion

        #region Down
        /// <summary>
        /// Migriert zur vorherigen Version
        /// </summary>
        public override void Down()
        {
        }
        #endregion
    }
}