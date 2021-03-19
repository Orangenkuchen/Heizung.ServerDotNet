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

    /// <summary>
    /// DataRepository für Datenbankanfragen bezüglich der Heizung
    /// </summary>
    public class HeaterRepository
    {
        #region static
        /// <summary>
        /// Formatstring, welche ein DateTime so umwandelt, dass es von der Datenbank erkannt wird
        /// </summary>
        private static string databaseDateTimeFormatString = "YYYY-MM-DD HH:mm:ss";
        #endregion

        #region fields
        /// <summary>
        /// Die Verbindung zur Datenbank welche für die Zugriffe verwendet werden.
        /// </summary>
        private readonly MySqlConnection mySqlConnection;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse und fügt die Datenbankverbindung hinzu
        /// </summary>
        /// <param name="mySqlConnection">Die Datenbank auf die Zugegriffen werden soll</param>
        public HeaterRepository(MySqlConnection mySqlConnection)
        {
            this.mySqlConnection = mySqlConnection;
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
            
            try
            {
                this.mySqlConnection.Open();

                var rows = this.mySqlConnection.Query("SELECT * FROM Heizung.ValueDescription;");

                foreach(var row in rows)
                {
                    result.Add(new ValueDescription() {
                        Id = row.Id,
                        Description = row.Description,
                        Unit = row.Unit,
                        IsLogged = row.IsLogged[0]
                    });
                }
            }
            finally
            {
                this.mySqlConnection.Close();
            }

            return result;
        }
        #endregion

        #region GetAllErrorValues
        /// <summary>
        /// Ermittelt alle Fehlerwerte aus der Datenbank
        /// </summary>
        /// <returns>Gibt ein Promise für die Fehlerwerte zurück</returns>
        /// <exception type="exception">Wird geworfen, wenn keine Verbindung mit der Datenbank hergestellt werden kann</exception>
        public IList<ErrorDescription> GetAllErrorValues() 
        {
            IList<ErrorDescription> result = new List<ErrorDescription>();
            
            try
            {
                this.mySqlConnection.Open();

                var rows = this.mySqlConnection.Query("SELECT * FROM Heizung.ValueDescription;");

                foreach(var row in rows)
                {
                    result.Add(new ErrorDescription() {
                        Id = row.Id,
                        Description = row.Description
                    });
                }
            }
            finally
            {
                this.mySqlConnection.Close();
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
            this.mySqlConnection.Open();
            
            try
            {
                var sql = $"SELECT * FROM Heizung.DataValues WHERE Timestamp BETWEEN @fromDate AND @toDate";

                var rows = this.mySqlConnection.Query(sql, new { fromDate = fromDate, toDate = toDate });

                foreach(var row in rows)
                {
                    result.Add(new DataValue() {
                        Id = row.Id,
                        TimeStamp = row.TimeStamp,
                        Value = row.Value,
                        ValueType = row.ValueTpye
                    });
                }
            }
            finally
            {
                this.mySqlConnection.Close();
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
            
            try
            {
                this.mySqlConnection.Open();

                var fromDate = DateTime.Now.AddHours(-2);

                var sql = $"SELECT * FROM DataValues WHERE Timestamp > @fromDate;";

                var rows = this.mySqlConnection.Query(sql, new { fromDate = fromDate });

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
                this.mySqlConnection.Close();
            }

            return result;
        }
        #endregion

        #region SetLoggingStateOfVaueType
        /// <summary>
        /// Erzeugt einen neuen Eintrag in der FehlerTabelle
        /// </summary>
        /// <param name="valueTypeList">errorText Der Fehlertext von der neuen Fehlermeldung</param>
        public void SetLoggingStateOfVaueType(IList<KeyValuePair<int, bool>> valueTypeList)
        {
            try
            {
                this.mySqlConnection.Open();

                using (var mySqlCommand = this.mySqlConnection.CreateCommand())
                {
                    var sqlStringBuilder = new StringBuilder(60);
                    sqlStringBuilder.Append("UPDATE TABLE 'ValueDescription' (Id, IsLogged) VALUE ");

                    var isFirst = true;

                    for (var i = 0; i < valueTypeList.Count; i++)
                    {
                        var valueType = valueTypeList[i];

                        if (isFirst == true)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            sqlStringBuilder.Append(", ");
                        }

                        sqlStringBuilder.AppendFormat("(@id{0}, @value{0})", i);

                        mySqlCommand.Parameters.AddWithValue("@id" + i, valueType.Key);
                        mySqlCommand.Parameters.AddWithValue("@value" + i, valueType.Value);
                    }

                    mySqlCommand.CommandText = sqlStringBuilder.ToString();
                    mySqlCommand.ExecuteNonQuery();
                }
            }
            finally
            {
                this.mySqlConnection.Close();
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

            try
            {
                this.mySqlConnection.Open();

                var lowerThresholdQueryTask = this.mySqlConnection.QueryAsync("SELECT LowerThreshold FROM NotifierConfig;");
                var notifierMailsQueryTask = this.mySqlConnection.QueryAsync("SELECT Mail FROM NotifierMails;");

                Task.WaitAll(lowerThresholdQueryTask, notifierMailsQueryTask);

                IList<Exception> exceptions = new List<Exception>();

                if (lowerThresholdQueryTask.IsFaulted)
                {
                    exceptions.Add(lowerThresholdQueryTask.Exception);
                }

                if (notifierMailsQueryTask.IsFaulted)
                {
                    exceptions.Add(notifierMailsQueryTask.Exception);
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
                        result.MailConfigs.Add(new MailConfig() 
                        {
                            Mail = row.Mail
                        });
                    }
                }
            }
            finally
            {
                this.mySqlConnection.Close();
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

            try
            {
                this.mySqlConnection.Open();

                var updateNotifierConfigQueryTask = this.mySqlConnection.ExecuteAsync($"UPDATE NotifierConfig SET LowerThreshold = @lowerThreshhold", new { lowerThreshhold = notifierConfig.LowerThreshold });
                
                this.mySqlConnection.Execute("TRUNCATE TABLE NotifierMails");

                using (var insertCommand = this.mySqlConnection.CreateCommand())
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
                    throw updateNotifierConfigQueryTask.Exception;
                }
            }
            finally
            {
                this.mySqlConnection.Close();
            }
        }
        #endregion

        #region GetAllErrorHashtable
        /// <summary>
        /// Ermittelt eine Hashtable mit allen Fehlern
        /// </summary>
        /// <exception type="Exception">Wird geworfen, wenn ein Datenbankfehler auftritt</exception>
        /// <returns>Gibt die Fehler als Dictionary zurück</returns>
        public IDictionary<int, string> GetAllErrorHashtable()
        {
            IDictionary<int, string> result = new Dictionary<int, string>();

            try
            {
                this.mySqlConnection.Open();

                var rows = this.mySqlConnection.Query("SELECT * FROM Heizung.ErrorList");

                foreach (var row in rows)
                {
                    if (result.ContainsKey(row.Id) == false)
                    {
                        result.Add(row.Id, row.Description);
                    }
                }
            }
            finally
            {
                this.mySqlConnection.Close();
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

            try
            {
                this.mySqlConnection.Open();

                var transaction = this.mySqlConnection.BeginTransaction();

                this.mySqlConnection.Execute($"INSERT INTO 'ErrorList' (Description) VALUE (@errorText)", param: new { errorText = errorText}, transaction: transaction);
                result = this.mySqlConnection.QuerySingle<int>("Select LAST_INSERT_ID()", transaction);

                transaction.Commit();
            }
            finally
            {
                this.mySqlConnection.Close();
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

            try
            {
                this.mySqlConnection.Open();

                using (var insertCommand = this.mySqlConnection.CreateCommand())
                {
                    var sqlStringBuilder = new StringBuilder(80);
                    sqlStringBuilder.Append("INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES ");

                    for (var i = 0; i < heaterDataDictonary.Count; i++)
                    {
                        var isFirst = true;

                        for (var j = 0; j < heaterDataDictonary[i].Data.Count; j++)
                        {
                            if (isFirst == true)
                            {
                                isFirst = false;
                            }
                            else
                            {
                                sqlStringBuilder.Append(", ");
                            }

                            sqlStringBuilder.AppendFormat("@valueType{0}x{1}, @value{0}x{1}, @timestamp{0}x{1}", i, j);
                            insertCommand.Parameters.AddWithValue($"@valueType{i}x{j}", heaterDataDictonary[i].ValueTypeId);
                            insertCommand.Parameters.AddWithValue($"@value{i}x{j}", heaterDataDictonary[i].Data[j].Value);
                            insertCommand.Parameters.AddWithValue($"@timestamp{i}x{j}", heaterDataDictonary[i].Data[j].TimeStamp);
                        }
                    }

                    insertCommand.CommandText = sqlStringBuilder.ToString();
                    insertCommand.ExecuteNonQuery();
                }

                this.mySqlConnection.Execute($"INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES {string.Join(", ", insertValues)}");
            }
            finally
            {
                this.mySqlConnection.Close();
            }
        }
        #endregion
    }
}