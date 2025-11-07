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
    using Microsoft.Extensions.Logging;
    using System.Threading;

    /// <summary>
    /// DataRepository für Datenbankanfragen bezüglich der Heizung
    /// </summary>
    public class HeaterRepository : IHeaterRepository
    {
        #region fields
        /// <summary>
        /// Der Verbindungsstring zur Datenbank
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Service zum Loggen von Nachrichten.
        /// </summary>
        private readonly ILogger logger;
        #endregion

        #region ctor
        /// <summary>
        /// Initialisiert die Klasse und fügt die Datenbankverbindung hinzu
        /// </summary>
        /// <param name="connectionString">Der Verbindungsstring zur Datenbank</param>
        /// <param name="logger">Service zum Loggen von Nachrichten.</param>
        public HeaterRepository(string connectionString, ILogger logger)
        {
            this.connectionString = connectionString;
            this.logger = logger;
        }
        #endregion

        #region GetAllValueDescriptions
        /// <inheritdoc />
        public async Task<IList<ValueDescription>> GetAllValueDescriptions(CancellationToken cancellationToken)
        {
            IList<ValueDescription> result = new List<ValueDescription>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var rows = await connection.QueryAsync<ValueDescription>(
                        new CommandDefinition(
                            "SELECT * FROM ValueDescription;",
                            cancellationToken: cancellationToken
                        )
                    );

                    foreach (var row in rows)
                    {
                        if (string.IsNullOrWhiteSpace(row.Unit))
                        {
                            row.Unit = null;
                        }

                        result.Add(row);
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region GetDataValues
        /// <inheritdoc />
        public async Task<IList<DataValue>> GetDataValues(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken)
        {
            IList<DataValue> result = new List<DataValue>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                await connection.OpenAsync();

                try
                {
                    var rows = await connection.QueryAsync<DataValuesRow>(
                        new CommandDefinition(
                            $"SELECT * FROM DataValues WHERE Timestamp BETWEEN @FromDate AND @ToDate",
                            new { FromDate = fromDate, ToDate = toDate },
                            cancellationToken: cancellationToken
                        )
                    );

                    foreach (var row in rows)
                    {
                        result.Add(new DataValue(row.Value)
                        {
                            Id = (int)row.Id,
                            TimeStamp = row.Timestamp,
                            ValueType = Convert.ToInt32(row.ValueType)
                        });
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region GetLatestDataValues
        /// <inheritdoc />
        public async Task<IDictionary<string, DataValue>> GetLatestDataValues(CancellationToken cancellationToken)
        {
            IDictionary<string, DataValue> result = new Dictionary<string, DataValue>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var fromDate = DateTime.Now.AddHours(-2);

                    var rows = await connection.QueryAsync<DataValuesRow>(
                        new CommandDefinition(
                            $"SELECT * FROM DataValues WHERE Timestamp > @fromDate;",
                            new { fromDate = fromDate },
                            cancellationToken: cancellationToken
                        )
                    );

                    foreach (var row in rows)
                    {
                        var overrideValue = false;

                        if (result.ContainsKey(row.ValueType.ToString()) == false)
                        {
                            overrideValue = true;
                        }
                        else if (result[row.ValueType.ToString()].TimeStamp < row.Timestamp)
                        {
                            overrideValue = true;
                        }

                        if (overrideValue == true)
                        {
                            result[row.ValueType.ToString()] = new DataValue(row.Value)
                            {
                                Id = Convert.ToInt32(row.Id),
                                TimeStamp = row.Timestamp,
                                ValueType = Convert.ToInt32(row.ValueType)
                            };
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region SetLoggingStateOfVaueType
        /// <inheritdoc />
        public async Task<bool> SetLoggingStateOfVaueType(IList<LoggingState> loggingStates, CancellationToken cancellationToken)
        {
            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var parameters = new Dictionary<string, object?>();

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

                        parameters.Add("@id" + i, loggingState.ValueTypeId);
                        parameters.Add("@value" + i, loggingState.IsLoged);
                    }

                    var rowsChanged = await connection.ExecuteAsync(
                        new CommandDefinition(
                            sqlStringBuilder.ToString(),
                            cancellationToken: cancellationToken
                        )
                    );

                    return rowsChanged > 0;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        #endregion

        #region GetMailNotifierConfig
        /// <inheritdoc />
        public async Task<NotifierConfig> GetMailNotifierConfig(CancellationToken cancellationToken)
        {
            var result = new NotifierConfig()
            {
                MailConfigs = new List<MailConfig>()
            };

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var transaction = await connection.BeginTransactionAsync();

                    var lowerThreshold = await connection.ExecuteScalarAsync<double?>(
                        new CommandDefinition(
                            "SELECT LowerThreshold FROM NotifierConfig",
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    if (lowerThreshold.HasValue == false)
                    {
                        throw new Exception("LowerThreshold was not found in table \"NotifierConfig\"");
                    }

                    var notifierMails = await connection.QueryAsync<string>(
                        new CommandDefinition(
                            "SELECT Mail FROM NotifierMails",
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    if (notifierMails.Count()  == 0)
                    {
                        throw new Exception("No Mail was not found in table \"NotifierMails\"");
                    }

                    foreach (var mail in notifierMails)
                    {
                        result.MailConfigs.Add(new MailConfig(mail));
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region SetMailNotifierConfig
        /// <inheritdoc />
        public async Task<bool> SetMailNotifierConfig(NotifierConfig notifierConfig, CancellationToken cancellationToken)
        {
            List<string> valuesList = new List<string>();
            for (var i = 0; i < notifierConfig.MailConfigs.Count; i++)
            {
                valuesList.Add($"({i}, '{notifierConfig.MailConfigs[i].Mail}')");
            }

            using (MySqlConnection connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var transaction = await connection.BeginTransactionAsync();

                    var rowsChanged = await connection.ExecuteAsync(
                        new CommandDefinition(
                            $"UPDATE NotifierConfig SET LowerThreshold = @lowerThreshhold",
                            new { lowerThreshhold = notifierConfig.LowerThreshold },
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    if (rowsChanged == 0)
                    {
                        return false;
                    }

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            "TRUNCATE TABLE NotifierMails",
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    var parameters = new Dictionary<string, object?>();

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
                        parameters.Add("@id" + i, i);
                        parameters.Add("@mail" + i, mailConfig.Mail);
                    }

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            stringBuilder.ToString(),
                            parameters,
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    if (cancellationToken.IsCancellationRequested == false)
                    {
                        await transaction.CommitAsync();
                    }

                    return true;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        #endregion

        #region GetAllErrorDictionary
        /// <inheritdoc />
        public async Task<IDictionary<int, ErrorDescription>> GetAllErrorDictionary(CancellationToken cancellationToken)
        {
            IDictionary<int, ErrorDescription> result = new Dictionary<int, ErrorDescription>();

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var rows = await connection.QueryAsync<ErrorDescription>(
                        new CommandDefinition(
                            "SELECT * FROM Heizung.ErrorList",
                            cancellationToken: cancellationToken
                        )
                    );

                    foreach (var row in rows)
                    {
                        if (result.ContainsKey(row.Id) == false)
                        {
                            result.Add(row.Id, row);
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region GetOperatingHoures
        /// <inheritdoc />
        public async Task<IList<DayOperatingHoures>> GetOperatingHoures(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            const string sql = "SELECT * FROM OperatingHoures WHERE `Timestamp` BETWEEN @FromDate AND @ToDate";
            IList<DayOperatingHoures> result = new List<DayOperatingHoures>();

            from = from.Date; // Anfang des Tags ermitteln
            to = to.Date.AddDays(1).AddMilliseconds(-1); // Ende des Tags ermitteln

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var command = new CommandDefinition(sql, new { FromDate = from, ToDate = to }, cancellationToken: cancellationToken);

                    var rows = await connection.QueryAsync<OperatingHouresRow>(command);

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
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region SetNewError
        /// <inheritdoc />
        public async Task<int> SetNewError(string errorText, CancellationToken cancellationToken)
        {
            var result = 0;

            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var transaction = await connection.BeginTransactionAsync();

                    var insertCommand = new CommandDefinition(
                        $"INSERT INTO ErrorList (Description) VALUE (@errorText)",
                        parameters: new { errorText = errorText },
                        transaction: transaction,
                        cancellationToken: cancellationToken
                    );

                    await connection.ExecuteAsync(insertCommand);

                    result = await connection.QuerySingleAsync<int>(
                        new CommandDefinition(
                            "Select LAST_INSERT_ID()",
                            transaction: transaction,
                            cancellationToken: cancellationToken
                        )
                    );

                    if (cancellationToken.IsCancellationRequested == false)
                    {
                        await transaction.CommitAsync();
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            return result;
        }
        #endregion

        #region SetHeaterValue
        /// <inheritdoc />
        public async Task<bool> SetHeaterValue(IEnumerable<HeaterData> heaterDataList, CancellationToken cancellationToken)
        {
            using (var connection = new MySqlConnection(this.connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var parameters = new Dictionary<string, object?>();

                    var sqlStringBuilder = new StringBuilder(80);
                    sqlStringBuilder.Append("INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES ");

                    var isFirst = true;

                    var counter = 0;
                    foreach (var element in heaterDataList)
                    {
                        for (var j = 0; j < element.Data.Count; j++)
                        {
                            if (element.Data[j].TimeStamp > new DateTime(2000, 1, 1))
                            {
                                if (isFirst == true)
                                {
                                    isFirst = false;
                                }
                                else
                                {
                                    sqlStringBuilder.Append(", ");
                                }

                                sqlStringBuilder.AppendFormat("(@valueType{0}, @value{0}, @timestamp{0})", counter);
                                parameters.Add($"@valueType{counter}", element.ValueTypeId);
                                parameters.Add($"@value{counter}", element.Data[j].Value);
                                parameters.Add($"@timestamp{counter}", element.Data[j].TimeStamp);
                            }

                            counter++;
                        }
                    }

                    var command = new CommandDefinition(sqlStringBuilder.ToString(), parameters, cancellationToken: cancellationToken);

                    var insertedRowsCount = await connection.ExecuteAsync(command);

                    return insertedRowsCount > 0;
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
        #endregion

        /// <summary>
        /// Stellt eine Zeile aus der Tabelle DataValues dar
        /// </summary>
        private struct DataValuesRow
        {
            /// <summary>
            /// Die Id vom Wert
            /// </summary>
            public uint Id { get; set; }

            /// <summary>
            /// Der Typ vom Heizungswert
            /// </summary>
            public uint ValueType { get; set; }

            /// <summary>
            /// Der Heizungswert
            /// </summary>
            public float Value { get; set; }

            /// <summary>
            /// Der Zeitpunkt vom Wert
            /// </summary>
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Stellt eine Zeile aus der Tabelle OperatingHours dar.
        /// </summary>
        private struct OperatingHouresRow
        {
            /// <summary>
            /// Der Zeitstempel vom Datenpunkt.
            /// </summary>
            public DateTime Timestamp { get; set; }

            /// <summary>
            /// Der Maximalwert vom Tag
            /// </summary>
            public float MaxDay { get; set; }

            /// <summary>
            /// Der Minimalwert vom Tag
            /// </summary>
            public float MinDay { get; set; }

            /// <summary>
            /// Die Anzahl der Stunde die an diesem Tag geheizt wurden
            /// </summary>
            public double Amount { get; set; }
        }
    }
}