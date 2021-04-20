namespace Heizung.ServerDotNet.Migrations
{
    using FluentMigrator;

    /// <summary>
    /// Fügt eine View zum Auflisten der Betriebstunden hinzu
    /// </summary>
    [Migration(2, "Fügt eine View zum Auflisten der Betriebstunden hinzu.")]
    public class _0002_OperatingHouresView : Migration
    {
        #region Up
        /// <summary>
        /// Migriert zur nächsten Version
        /// </summary>
        public override void Up()
        {
            base.Execute.Script("Migrations/SqlScripts/0002_OperatingHouresView.sql");
        }
        #endregion

        #region Down
        /// <summary>
        /// Migriert zur vorherigen Version
        /// </summary>
        public override void Down()
        {
            base.Execute.Script("DROP VIEW Heizung.OperatingHoures;");
        }
        #endregion
    }
}