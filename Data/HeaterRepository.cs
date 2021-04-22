namespace Heizung.ServerDotNet.Data
{
    using MySqlConnector;
    using Dapper;
    using System.Collections.Generic;
    using Heizung.ServerDotNet.Entities;
    using System;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Text;
    using FluentMigrator.Runner;

    /// <summary>
    /// DataRepository für Datenbankanfragen bezüglich der Heizung
    /// </summary>
    public class HeaterRepository : IHeaterRepository
    {
        #region static
        /// <summary>
        /// Formatstring, welche ein DateTime so umwandelt, dass es von der Datenbank erkannt wird
        /// </summary>
        private static string databaseDateTimeFormatString = "YYYY-MM-DD HH:mm:ss";
        #endregion

        #region fields
        /// <summary>
        /// Der Verbindungsstring zur Datenbank
        /// </summary>
        private readonly string connectionString;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse und fügt die Datenbankverbindung hinzu
        /// </summary>
        /// <param name="connectionString">Der Verbindungsstring zur Datenbank</param>
        public HeaterRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
        #endregion

        #region GetAllValueDescriptions
        /// <summary>
        /// Ermittelt alle DatenWert-Bescheibungen aus der Datenbank
        /// </summary>
        /// <returns>Gibt ein Promise für die DatenBeschreibungen zurück</returns>
        /// <exception type="exception">Wird geworfen, wenn keine Verbindung mit der Datenbank hergestellt werden kann</exception>
        public IList<ValueDescription> GetAllValueDescriptions() 
        {
            IList<ValueDescription> result = new List<ValueDescription>();
            
            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    var rows = connection.Query("SELECT * FROM Heizung.ValueDescription;");

                    foreach(var row in rows)
                    {
                        result.Add(new ValueDescription(row.Description, row.Unit) {
                            Id = row.Id,
                            IsLogged = Convert.ToBoolean(row.IsLogged)
                        });
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region GetDataValues
        /// <summary>
        /// Ermittelt alle DatenWerte innherhalb der Zeit aus der Datenbank
        /// </summary>
        /// <param name="fromDate">Der Zeitpunkt, von dem an die Daten ermittelt werden sollen</param>
        /// <param name="toDate">Der Zeitpunkt, bis zu dem die Daten ermittelt werden sollen</param>
        /// <returns>Gibt die Daten zurück</returns>
        public IList<DataValue> GetDataValues(DateTime fromDate, DateTime toDate) 
        {
            IList<DataValue> result = new List<DataValue>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                connection.Open();

                try
                {
                    var sql = $"SELECT * FROM Heizung.DataValues WHERE Timestamp BETWEEN @FromDate AND @ToDate";

                    var rows = connection.Query(sql, new { FromDate = fromDate, ToDate = toDate });

                    foreach(var row in rows)
                    {
                        result.Add(new DataValue(row.Value) {
                            Id = (int)row.Id,
                            TimeStamp = row.Timestamp,
                            ValueType = row.ValueType
                        });
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region GetLatestDataValues
        /// <summary>
        /// Ermittelt die DatenWerte neusten Datenwerte (für jeden DatenTyp) aus der Datenbank
        /// </summary>
        /// <returns>Gibt die Daten zurück</returns>
        public IDictionary<string, DataValue> GetLatestDataValues() 
        {
            IDictionary<string, DataValue> result = new Dictionary<string, DataValue>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    var fromDate = DateTime.Now.AddHours(-2);

                    var sql = $"SELECT * FROM DataValues WHERE Timestamp > @fromDate;";

                    var rows = connection.Query(sql, new { fromDate = fromDate });

                    foreach (var row in rows)
                    {
                        var overrideValue = false;

                        if (result.ContainsKey(row.ValueType.ToString()) == false)
                        {
                            overrideValue = true;
                        }
                        else if (result[row.ValueType.ToString()].TimeStamp < row.TimeStamp)
                        {
                            overrideValue = true;
                        }

                        if (overrideValue == true) 
                        {
                            result[row.ValueType.ToString()] = row;
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region SetLoggingStateOfVaueType
        /// <summary>
        /// Stellt ein, welche Heizungswerte in der Historie gespeichert werden sollen
        /// </summary>
        /// <param name="loggingStates">Die Einstellung welche gesetzt werden soll</param>
        public void SetLoggingStateOfVaueType(IList<LoggingState> loggingStates)
        {
            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    using (var mySqlCommand = connection.CreateCommand())
                    {
                        var sqlStringBuilder = new StringBuilder(60);
                        sqlStringBuilder.Append("UPDATE TABLE 'ValueDescription' (Id, IsLogged) VALUE ");

                        var isFirst = true;

                        for (var i = 0; i < loggingStates.Count; i++)
                        {
                            var loggingState = loggingStates[i];

                            if (isFirst == true)
                            {
                                isFirst = false;
                            }
                            else
                            {
                                sqlStringBuilder.Append(", ");
                            }

                            sqlStringBuilder.AppendFormat("(@id{0}, @value{0})", i);

                            mySqlCommand.Parameters.AddWithValue("@id" + i, loggingState.ValueTypeId);
                            mySqlCommand.Parameters.AddWithValue("@value" + i, loggingState.IsLoged);
                        }

                        mySqlCommand.CommandText = sqlStringBuilder.ToString();
                        mySqlCommand.ExecuteNonQuery();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        #endregion

        #region GetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <returns>Git die NotifierConfig zurück</returns>
        public NotifierConfig GetMailNotifierConfig()
        {
            var result = new NotifierConfig()
            {
                MailConfigs = new List<MailConfig>()
            };

            var from = DateTime.Now.AddHours(-2);
            IList<string> valuesStrings = new List<string>();

            using (var connectionA = new MySqlConnection(this.connectionString))
            {            
                using (var connectionB = new MySqlConnection(this.connectionString))
                {
                    try
                    {
                        connectionA.Open();
                        connectionB.Open();

                        var lowerThresholdQueryTask = connectionA.QueryAsync("SELECT LowerThreshold FROM NotifierConfig;");
                        var notifierMailsQueryTask = connectionB.QueryAsync("SELECT Mail FROM NotifierMails;");

                        Task.WaitAll(lowerThresholdQueryTask, notifierMailsQueryTask);

                        IList<Exception> exceptions = new List<Exception>();

                        if (lowerThresholdQueryTask.IsFaulted)
                        {
                            exceptions.Add(lowerThresholdQueryTask.Exception ?? new Exception());
                        }

                        if (notifierMailsQueryTask.IsFaulted)
                        {
                            exceptions.Add(notifierMailsQueryTask.Exception ?? new Exception());
                        }

                        if (exceptions.Count > 0)
                        {
                            throw new AggregateException("Beim ermitteln von der MailConfig ist mindestens ein Fehler aufgetreten", exceptions);
                        }

                        if (lowerThresholdQueryTask.Result.Count() > 0)
                        {
                            result.LowerThreshold = lowerThresholdQueryTask.Result.ElementAt(0).LowerThreshold;

                            foreach(var row in notifierMailsQueryTask.Result)
                            {
                                result.MailConfigs.Add(new MailConfig(row.Mail));
                            }
                        }
                    }
                    finally
                    {
                        connectionA.Close();
                        connectionB.Close();
                    }
                }
            }

            return result;
        }
        #endregion

        #region SetMailNotifierConfig
        /// <summary>
        /// Ermittelt die NotifierConfig aus der Datenbank
        /// </summary>
        /// <param name="notifierConfig">Die Konfiguration, welche gespeichert werden soll</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        public void SetMailNotifierConfig(NotifierConfig notifierConfig)
        {
            List<string> valuesList = new List<string>();
            for (var i = 0; i < notifierConfig.MailConfigs.Count; i++)
            {
                valuesList.Add($"({i}, '{notifierConfig.MailConfigs[i].Mail}')");
            }

            using (MySqlConnection connectionA = new MySqlConnection(this.connectionString))
            {
                using (MySqlConnection connectionB = new MySqlConnection(this.connectionString))
                {
                    try
                    {
                        connectionA.Open();
                        connectionB.Open();

                        var updateNotifierConfigQueryTask = connectionA.ExecuteAsync($"UPDATE NotifierConfig SET LowerThreshold = @lowerThreshhold", new { lowerThreshhold = notifierConfig.LowerThreshold });
                        
                        connectionB.Execute("TRUNCATE TABLE NotifierMails");

                        using (var insertCommand = connectionB.CreateCommand())
                        {
                            var stringBuilder = new StringBuilder(60);
                            stringBuilder.Append("INSERT INTO NotifierMails (Id, Mail) VALUES ");
                            
                            var isFirst = true;

                            for (var i = 0; i < notifierConfig.MailConfigs.Count; i++)
                            {
                                var mailConfig = notifierConfig.MailConfigs[i];

                                if (isFirst == true)
                                {
                                    isFirst = false;
                                }
                                else
                                {
                                    stringBuilder.Append(", ");
                                }

                                stringBuilder.AppendFormat("(@id{0}, @mail{0})", i);
                                insertCommand.Parameters.AddWithValue("@id" + i, i);
                                insertCommand.Parameters.AddWithValue("@mail" + i, mailConfig.Mail);
                            }

                            insertCommand.CommandText = stringBuilder.ToString();
                            insertCommand.ExecuteNonQuery();
                        }

                        updateNotifierConfigQueryTask.Wait();

                        if (updateNotifierConfigQueryTask.IsFaulted)
                        {
                            throw updateNotifierConfigQueryTask.Exception ?? new Exception();
                        }
                    }
                    finally
                    {
                        connectionA.Close();
                        connectionB.Close();
                    }
                }
            }
        }
        #endregion

        #region GetAllErrorDictionary
        /// <summary>
        /// Ermittelt eine Dictionary mit allen Fehlern
        /// </summary>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Fehler als Dictionary zurück</returns>
        public IDictionary<int, ErrorDescription> GetAllErrorDictionary()
        {
            IDictionary<int, ErrorDescription> result = new Dictionary<int, ErrorDescription>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    var rows = connection.Query("SELECT * FROM Heizung.ErrorList");

                    foreach (var row in rows)
                    {
                        if (result.ContainsKey((int)row.Id) == false)
                        {
                            result.Add((int)row.Id, new ErrorDescription(row.Description)
                            {
                                Id = (int)row.Id
                            });
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region GetOperatingHoures
        /// <summary>
        /// Ermittelt eine Liste von täglichen Betriebsstunden im angegeben Zeitraum
        /// </summary>
        /// <param name="from">Von welchem Datum an geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <param name="to">Bis zu welchem Zeitpunkt geholt werden soll. (Nur Datum wird beachtet nicht Uhrzeit)</param>
        /// <returns>Gibt die Liste der ermittelten Einträge zurück</returns>
        public IList<DayOperatingHoures> GetOperatingHoures(DateTime from, DateTime to)
        {
            const string sql = "SELECT * FROM OperatingHoures WHERE `Timestamp` BETWEEN @FromDate AND @ToDate";
            IList<DayOperatingHoures> result = new List<DayOperatingHoures>();

            from = from.Date; // Anfang des Tags ermitteln
            to = to.Date.AddDays(1).AddMilliseconds(-1); // Ende des Tags ermitteln

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    var rows = connection.Query(sql, new { FromDate= from, ToDate=to });

                    foreach (var row in rows)
                    {
                        result.Add(new DayOperatingHoures()
                        {
                            Date = row.Timestamp.Date,
                            MinHoures = Convert.ToUInt32(row.MinDay),
                            MaxHoures = Convert.ToUInt32(row.MaxDay),
                            Houres = Convert.ToUInt32(row.Amount)
                        });
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region SetNewError
        /// <summary>
        /// Erzeugt einen neuen Eintrag in der FehlerTabelle
        /// </summary>
        /// <param name="errorText">Der Fehlertext von der neuen Fehlermeldung</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        public int SetNewError(string errorText)
        {
            var result = 0;

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    var transaction = connection.BeginTransaction();

                    connection.Execute($"INSERT INTO 'ErrorList' (Description) VALUE (@errorText)", param: new { errorText = errorText}, transaction: transaction);
                    result = connection.QuerySingle<int>("Select LAST_INSERT_ID()", transaction);

                    transaction.Commit();
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        #endregion

        #region SetHeaterValue
        /// <summary>
        /// Fügt neue Heizwerte in die Tabelle hinzu
        /// </summary>
        /// <param name="heaterDataDictonary">Dictionary mit den Werten, welche hinzugefügt werden sollen</param>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Id von dem neuen Eintrag zurück</returns>
        public void SetHeaterValue(IDictionary<int, HeaterData> heaterDataDictonary)
        {
            IList<string> insertValues = new List<string>();
            var currentDate = DateTime.Now;

            foreach (var heaterData in heaterDataDictonary)
            {
                foreach (var dataPoint in heaterData.Value.Data)
                {
                   insertValues.Add($"({heaterData.Value.ValueTypeId}, {dataPoint.Value}, {dataPoint.TimeStamp.ToString(databaseDateTimeFormatString)})");
                }
            }

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    connection.Open();

                    using (var insertCommand = connection.CreateCommand())
                    {
                        var sqlStringBuilder = new StringBuilder(80);
                        sqlStringBuilder.Append("INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES ");

                        var isFirst = true;

                        foreach (var keyValuePair in heaterDataDictonary)
                        {
                            for (var j = 0; j < heaterDataDictonary[keyValuePair.Key].Data.Count; j++)
                            {
                                if (heaterDataDictonary[keyValuePair.Key].Data[j].TimeStamp > new DateTime(2000, 1, 1))
                                {
                                    if (isFirst == true)
                                    {
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        sqlStringBuilder.Append(", ");
                                    }

                                    sqlStringBuilder.AppendFormat("(@valueType{0}x{1}, @value{0}x{1}, @timestamp{0}x{1})", keyValuePair.Key, j);
                                    insertCommand.Parameters.AddWithValue($"@valueType{keyValuePair.Key}x{j}", heaterDataDictonary[keyValuePair.Key].ValueTypeId);
                                    insertCommand.Parameters.AddWithValue($"@value{keyValuePair.Key}x{j}", heaterDataDictonary[keyValuePair.Key].Data[j].Value);
                                    insertCommand.Parameters.AddWithValue($"@timestamp{keyValuePair.Key}x{j}", DateTime.Now);
                                }
                            }
                        }

                        insertCommand.CommandText = sqlStringBuilder.ToString();
                        insertCommand.ExecuteNonQuery();
                    }

                    //connection.Execute($"INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES {string.Join(", ", insertValues)}");
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        #endregion
    }
}