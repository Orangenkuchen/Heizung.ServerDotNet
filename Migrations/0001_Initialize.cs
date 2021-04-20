namespace Heizung.ServerDotNet.Migrations
{
    using FluentMigrator;

    /// <summary>
    /// Migration, welche die Grundversion von der Datenbank erstellt
    /// </summary>
    [Migration(1, "Legt die Grundstrucktur in der Datenbank an.")]
    public class _0001_Initialize : Migration
    {
        #region Up
        /// <summary>
        /// Migriert zur nächsten Version
        /// </summary>
        public override void Up()
        {
            base.Create.Table("DataValues")
                        .WithDescription("Tabelle mit den Datenwerten, welche von der Heizung empfangen und gespeichert wurden.")
                        .WithColumn("Id")
                            .AsInt32()
                            .NotNullable()
                            .PrimaryKey()
                            .Identity()
                            .Indexed()
                            .Unique()
                            .WithColumnDescription("Die Id vom Datensatz.")
                        .WithColumn("ValueType")
                            .AsByte()
                            .NotNullable()
                            .Indexed()
                            .WithColumnDescription("Der Typ vom Heizungswert.")
                        .WithColumn("Value")
                            .AsFloat()
                            .Nullable()
                            .WithDefaultValue(null)
                            .WithColumnDescription("Der Wert von der Heizung.")
                        .WithColumn("Timestamp")
                            .AsDateTime2()
                            .NotNullable()
                            .Indexed()
                            .WithColumnDescription("Der Zeitstempel vom Datenpunkt.");
            
            base.Create.Table("ErrorList")
                        .WithDescription("In dieser Tabelle werden die Fehlerbeschreibungen, welche von der Heizung kommen, gespeichert.")
                        .WithColumn("Id")
                            .AsInt32()
                            .PrimaryKey()
                            .Unique()
                            .NotNullable()
                            .Indexed()
                            .WithColumnDescription("Die Id vom Error.")
                        .WithColumn("Description")
                            .AsFixedLengthString(100)
                            .Nullable()
                            .WithDefaultValue(null)
                            .WithColumnDescription("Die Beschreibung vom Fehler, welche von der Heizung kommt.");
            
            base.Create.Table("HeatingStates")
                        .WithDescription("Tabelle mit den Beschreibungen der Status welche die Heizung annehmen kann.")
                        .WithColumn("Id")
                            .AsInt32()
                            .PrimaryKey()
                            .Unique()
                            .NotNullable()
                            .Indexed()
                            .WithColumnDescription("Die Id vom Heizungsstatus.")
                        .WithColumn("Description")
                            .AsFixedLengthString(30)
                            .NotNullable()
                            .WithColumnDescription("Die Beschreibung vom Heizstatus.");
            
            base.Create.Table("NotifierConfig")
                        .WithDescription("In dieser Tabelle werden die Konfigurationen für die Benachrichtigung der User per Mail konfiguriert.")
                        .WithColumn("Id")
                            .AsInt32()
                            .PrimaryKey()
                            .Unique()
                            .NotNullable()
                            .Indexed()
                            .Identity()
                            .WithColumnDescription("Die Id von der Konfiguration.")
                        .WithColumn("LowerThreshold")
                            .AsDouble()
                            .NotNullable()
                            .WithColumnDescription("Wenn diese Grenze unterschritten wird, werden die User benachrichtigt.");
            
            base.Create.Table("NotifierMails")
                        .WithDescription("In dieser Tabelle werden die Emailadresse gepsichert, welche vom Server benachrichtigt werden sollen.")
                        .WithColumn("Id")
                            .AsInt32()
                            .PrimaryKey()
                            .Unique()
                            .NotNullable()
                            .Indexed()
                            .Identity()
                            .WithColumnDescription("Die Id von der Mailkonfiguration.")
                        .WithColumn("Mail")
                            .AsFixedLengthString(100)
                            .NotNullable()
                            .WithColumnDescription("Die Emailadresse vom User.");

            base.Create.Table("ValueDescription")
                        .WithDescription("In dieser Tabelle werden die verschiedenen Typen von Wert, welche von der Heizung empfangen werden können, beschrieben. Hier wird auch festgelegt, welche davon in der Datenbank gepsichert werden.")
                        .WithColumn("Id")
                            .AsInt32()
                            .PrimaryKey()
                            .Unique()
                            .NotNullable()
                            .Indexed()
                            .Identity()
                            .WithColumnDescription("Die Id von der Typ-Beschreibung.")
                        .WithColumn("Description")
                            .AsFixedLengthString(100)
                            .NotNullable()
                            .WithColumnDescription("Beschreibt, was der Heizungstyp darstellt.")
                        .WithColumn("Unit")
                            .AsFixedLengthString(50)
                            .Nullable()
                            .WithDefaultValue(null)
                            .WithColumnDescription("Die Einheit von Wert. Ist Null, wenn der Wert keine Einheit hat.")
                        .WithColumn("IsLogged")
                            .AsBoolean()
                            .NotNullable()
                            .WithColumnDescription("Gibt an, ob dieser Wert in der Datenbank periodisch gespeichert werden soll.");
        }
        #endregion

        #region Down
        /// <summary>
        /// Migriert zur vorherigen Version
        /// </summary>
        public override void Down()
        {
            base.Delete.Table("DataValues");
            base.Delete.Table("ErrorList");
            base.Delete.Table("HeatingStates");
            base.Delete.Table("NotifierConfig");
            base.Delete.Table("NotifierMails");
            base.Delete.Table("ValueDescription");
        }
        #endregion
    }
}