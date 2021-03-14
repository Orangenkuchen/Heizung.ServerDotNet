using MySqlConnector;
using Dapper;
using System.Collections.Generic;
using Heizung.ServerDotNet.Entities;

namespace Heizung.ServerDotNet.Data
{
    /// <summary>
    /// DataRepository für Datenbankanfragen bezüglich der Heizung
    /// </summary>
    public class HeaterRepository
    {
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
            this.mySqlConnection.Open();
            
            try
            {
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
            this.mySqlConnection.Open();
            
            try
            {
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
    }
}

    // #region GetDataValues
    /**
     * Ermittelt alle DatenWerte innherhalb der Zeit aus der Datenbank
     * 
     * @param fromDate Der Zeitpunkt, von dem an die Daten ermittelt werden sollen
     * @param toDate Der Zeitpunkt, bis zu dem die Daten ermittelt werden sollen
     * @returns Gibt ein Promise für die Daten zurück
     */
    public GetDataValues(fromDate: Date, toDate: Date): Promise<Array<DataValue>> {
        let sql = `SELECT * FROM Heizung.DataValues WHERE Timestamp BETWEEN "${this.DateToDBDateString(fromDate)}" AND "${this.DateToDBDateString(toDate)}";`;

        let result = new Promise<Array<DataValue>>((resolve, reject) => {
            let connectionPromise = this.connectionPool.getConnection();

            connectionPromise.then((connection) => {
                let queryResultPromise = connection.query(sql);
                queryResultPromise.then((rows: Array<DataValue>) => {
                    connection.release();

                    resolve(rows);
                });

                queryResultPromise.catch((exception) => {
                    try { 
                        connection.release()
                    } finally { 
                        reject(exception);
                    }
                });
            });
            connectionPromise.catch((exception) => reject(exception));
        });

        return result;
    }
    // #endregion

    // #region GetLatestDataValues
    /**
     * Ermittelt die DatenWerte neusten Datenwerte (für jeden DatenTyp) aus der Datenbank
     * 
     * @returns Gibt ein Promise für die Daten zurück
     */
    public GetLatestDataValues(): Promise<DataValueHashTable> {
        let from = new Date();
        from.setHours(from.getHours() - 2);
        let sql = `SELECT * FROM DataValues WHERE Timestamp > "${this.DateToDBDateString(from)}";`;

        let result = new Promise<DataValueHashTable>((resolve, reject) => {
            let connectionPromise = this.connectionPool.getConnection();

            connectionPromise.then((connection) => {
                let queryResultPromise = connection.query(sql);
                queryResultPromise.then((rows: Array<DataValue>) => {
                    connection.release();

                    let resultRows: DataValueHashTable = {};

                    rows.forEach((row) => {
                        let override = false;

                        if (typeof resultRows[row.ValueType.toString()] == "undefined") {
                            override = true;
                        } else if (resultRows[row.ValueType.toString()].TimeStamp < row.TimeStamp) {
                            override = true;
                        }

                        if (override == true) {
                            resultRows[row.ValueType.toString()] = row;
                        }
                    });

                    resolve(resultRows);
                });

                queryResultPromise.catch((exception) => {
                    try { 
                        connection.release()
                    } finally { 
                        reject(exception);
                    }
                });
            });
            connectionPromise.catch((exception) => reject(exception));
        });

        return result;
    }
    // #endregion

    // #region SetLoggingStateOfVaueType
    /**
     * Erzeugt einen neuen Eintrag in der FehlerTabelle
     * 
     * @param errorText Der Fehlertext von der neuen Fehlermeldung
     * @returns Gibt ein Promise mit der Id zurück
     */
    public SetLoggingStateOfVaueType(valueTypeArray: Array<{key: Number, state: Boolean}>): Promise<void> {
        let that = this;

        let valuesSqlStringArray = new Array<string>();

        valueTypeArray.forEach((valueType) => {
            valuesSqlStringArray.push(`${valueType.key}, ${valueType.state == true ? 1 : 0 }`);
        });

        let result: Promise<void> = new Promise<void>(function(resolve, reject) {
            let connectionPromise = that.connectionPool.getConnection();

            connectionPromise.then(connection => {
                let sql = `UPDATE TABLE 'ValueDescription' (Id, IsLogged) VALUE ${valuesSqlStringArray.join(", ")} ON DUPLICATE KEY UPDATE IsLogged=VALUES(IsLogged);`;
                let queryResultPromise = connection.query(sql);

                queryResultPromise.then(result => {
                    connection.release();
                    resolve();
                });
                
                queryResultPromise.catch((exception) => {
                    try { 
                        connection.release()
                    } finally { 
                        reject(exception);
                    }
                });
            });

            connectionPromise.catch(exception => reject(exception));
        });
        
        return result;
    }
    // #endregion

    // #region GetMailNotifierConfig
    /**
     * Ermittelt die NotifierConfig aus der Datenbank
     */
    public GetGetMailNotifierConfig(): Promise<NotifierConfig> {
        let that = this;
        let notifierConfig: NotifierConfig = { lowerThreshold: 0, mailConfigs: new Array<MailConfig>() };

        let result: Promise<NotifierConfig> = new Promise<NotifierConfig>((resolve, reject) => {
            let connectionPromise = that.connectionPool.getConnection();
            
            connectionPromise.then(connection => {
                let notifierConfigLowerThreshholdPromise = new Promise<number>((resolveNotifierConfig, rejectNotifierConfig) => {
                    let lowerThreshholdQuerryPromise = connection.query("SELECT LowerThreshold FROM NotifierConfig;");

                    lowerThreshholdQuerryPromise.then(rows => {
                        if (rows.length > 0) {
                            resolveNotifierConfig(rows[0].LowerThreshold);
                        }
                    });

                    lowerThreshholdQuerryPromise.catch(exception => rejectNotifierConfig());
                });

                let notifierMailListPromise = new Promise<Array<MailConfig>>((resolveNotifierMailList, rejectNotifierMailList) => {
                    let notifierMailListQuerryPromise = connection.query("SELECT Mail FROM NotifierMails;");

                    notifierMailListQuerryPromise.then(rows => {
                        let mailConfigs = new Array<MailConfig>();

                        rows.forEach((row) => {
                            mailConfigs.push(row);
                        });

                        resolveNotifierMailList(mailConfigs);
                    });

                    notifierMailListQuerryPromise.catch(exception => rejectNotifierMailList());
                });

                let notifierAllPromise = Promise.all([notifierConfigLowerThreshholdPromise, notifierMailListPromise]);

                notifierAllPromise.then((resultArray) => {
                    connection.release();

                    let [ notifierConfigLowerThreshhold, notifierMailList ] = resultArray;

                    resolve({
                        lowerThreshold: notifierConfigLowerThreshhold,
                        mailConfigs: notifierMailList
                    });
                });

                notifierAllPromise.catch((exception) => {
                    try { 
                        connection.release()
                    } finally { 
                        reject(exception);
                    }
                });
            });

            connectionPromise.catch(exception => reject());
        });

        return result;
    }
    // #endregion

    // #region SetMailNotifierConfig
    /**
     * Ermittelt die NotifierConfig aus der Datenbank
     */
    public SetMailNotifierConfig(notifierConfig: NotifierConfig): Promise<void> {
        let that = this;

        let valuesArray = [];
        for(var i = 0; i < notifierConfig.mailConfigs.length; i++) {
            valuesArray.push(`(${i}, '${notifierConfig.mailConfigs[i].Mail}')`);
        }

        let sqlQuerries = [];
        
        sqlQuerries.push("TRUNCATE TABLE NotifierMails");
        sqlQuerries.push(`INSERT INTO NotifierMails (Id, Mail) VALUES ${valuesArray.join(", ")}`);
        sqlQuerries.push(`UPDATE NotifierConfig SET LowerThreshold = ${notifierConfig.lowerThreshold}`);

        let result: Promise<void> = new Promise<void>((resolve, reject) => {
            that.connectionPool.getConnection()
                               .then(connection => {
                                    var querryPormiseArray = new Array<Promise<any>>();
                                    
                                    sqlQuerries.forEach((sql) => {
                                        querryPormiseArray.push(connection.query(sql));
                                    });

                                    Promise.all(querryPormiseArray).then((results) => {
                                        connection.release();
                                        resolve();
                                    }).catch(exception => {
                                        try {
                                            connection.release();
                                        } finally {
                                            reject();
                                        }
                                    });
                               });
        });

        return result;
    }
    // #endregion

    // #region DateToDBDateString
    /**
     * Wandelt ein Date in einen String um, welcher in SQL-Querrys verwendet werden kann
     * 
     * @param date Das Datum welches umgewandelt werden soll
     * @return Das Datum als DB-Query-String
     */
    private DateToDBDateString(date: Date): string {
        return `${date.getFullYear()}-${this.leftPad2(date.getMonth() + 1)}-${this.leftPad2(date.getDate())} ${this.leftPad2(date.getHours())}:${this.leftPad2(date.getMinutes())}:${this.leftPad2(date.getSeconds())}`;
    }
    // #endregion

    // #region GetAllErrorHashtable
    /**
     * Ermittelt eine Hashtable mit allen Fehlern.
     * 
     * @returns Gibt ein Promise mit der ErrorHashtable
     */
    public GetAllErrorHashtable(): Promise<ErrorHashTable> {
        let that = this;

        let result: Promise<ErrorHashTable> = new Promise<ErrorHashTable>(function(resolve, reject) {
            that.connectionPool.getConnection()
                                .then(connection => {
                                    connection.query(`SELECT * FROM Heizung.ErrorList`)
                                                .then(rows => {
                                                    connection.release();
                                                    let errorHashTable: ErrorHashTable = {};

                                                    rows.forEach((row) => {
                                                        errorHashTable[row.Id] = row.Description;
                                                    });
                                                    
                                                    resolve(errorHashTable);
                                                })
                                                .catch(exception => {
                                                    try {
                                                        connection.release();
                                                    } finally {
                                                        reject(exception);
                                                    }
                                                });
                                })
                                .catch(exception => reject(exception));
        });
        
        return result;
    }
    // #endregion

    // #region SetNewError
    /**
     * Erzeugt einen neuen Eintrag in der FehlerTabelle
     * 
     * @param errorText Der Fehlertext von der neuen Fehlermeldung
     * @returns Gibt ein Promise mit der Id zurück
     */
    public SetNewError(errorText: string): Promise<number> {
        let that = this;

        let result: Promise<number> = new Promise<number>(function(resolve, reject) {
            that.connectionPool.getConnection()
                            .then(connection => {
                                connection.query(`INSERT INTO 'ErrorList' (Description) VALUE ('${errorText}')`)
                                            .then(result => {
                                                connection.release();
                                                resolve(result.insertId);
                                            })
                                            .catch(exception => {
                                                try {
                                                    connection.release();
                                                } finally {
                                                    reject(exception);
                                                }
                                            });
                            })
                            .catch(exception => reject(exception));
        });
        
        return result;
    }
    // #endregion

    // #region SetHeaterValue
    /**
     * Fügt neue Heizwerte in die Tabelle hinzu
     * 
     * @param heaterDataHashMap Hashmap mit den Werten, welche hinzugefügt werden sollen
     * @returns Gibt ein Promise das angibt wenn der Insert abgeschlossen wurde
     */
    public SetHeaterValue(heaterDataHashMap: HeaterDataHashMap): Promise<Boolean> {
        let that = this;
        let sqlInsertValues = new Array<string>();
        let currentTime = new Date();
        let currentTimeString = `${currentTime.getFullYear()}-${this.leftPad2(currentTime.getMonth() + 1)}-${this.leftPad2(currentTime.getDate())} ${this.leftPad2(currentTime.getHours())}:${this.leftPad2(currentTime.getMinutes())}:${this.leftPad2(currentTime.getSeconds())}`;

        for(let valueTypeId in heaterDataHashMap) {
            let heaterValue = heaterDataHashMap[valueTypeId];

            heaterValue.data.forEach(dataPoint => {
                if (typeof dataPoint.value == "number") {
                    sqlInsertValues.push(`(${heaterValue.valueTypeId}, ${dataPoint.value}, '${currentTimeString}')`);
                } else {
                    sqlInsertValues.push(`(${heaterValue.valueTypeId}, '${dataPoint.value}', '${currentTimeString}')`);
                }
            });
        }

        let sql = `INSERT INTO Heizung.DataValues (ValueType, Value, Timestamp) VALUES ${sqlInsertValues.join(", ")}`;

        let result: Promise<Boolean> = new Promise<Boolean>(function(resolve, reject) {
            that.connectionPool.getConnection()
                            .then(connection => {
                                connection.query(sql)
                                            .then(result => {
                                                connection.release();
                                                resolve(true);
                                            })
                                            .catch(exception => {
                                                try {
                                                    connection.release();
                                                } finally {
                                                    reject(exception);
                                                }
                                            });
                            })
                            .catch(exception => reject(exception))
        });
        
        return result;
    }
    // #endregion

    // #region leftPad2