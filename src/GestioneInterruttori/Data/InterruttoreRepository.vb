Imports Microsoft.Data.Sqlite
Imports GestioneInterruttori.Models

Namespace Data
    Public Class InterruttoreRepository
        Private ReadOnly _db As InterruttoriDbContext

        Public Sub New(db As InterruttoriDbContext)
            _db = db
        End Sub

        Public Function ElencoMarche() As List(Of Marca)
            Dim risultato As New List(Of Marca)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT Id, Nome FROM Marca ORDER BY Nome"
                    Using lettore = comando.ExecuteReader()
                        While lettore.Read()
                            risultato.Add(New Marca With {
                                .Id = lettore.GetInt32(0),
                                .Nome = lettore.GetString(1)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return risultato
        End Function

        Public Function ElencoSerie() As List(Of Serie)
            Dim risultato As New List(Of Serie)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT Id, Nome FROM Serie ORDER BY Nome"
                    Using lettore = comando.ExecuteReader()
                        While lettore.Read()
                            risultato.Add(New Serie With {
                                .Id = lettore.GetInt32(0),
                                .Nome = lettore.GetString(1)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return risultato
        End Function

        ''' <summary>
        ''' Interruttori filtrabili per marca/serie/poli, secondo i filtri della maschera di selezione.
        ''' </summary>
        Public Function CercaInterruttori(Optional idMarca As Integer? = Nothing,
                                           Optional idSerie As Integer? = Nothing,
                                           Optional numeroPoli As Integer? = Nothing) As List(Of Interruttore)
            Dim risultato As New List(Of Interruttore)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    Dim sql As String = "SELECT Id, IdMarca, IdSerie, Nome, IdTipo, NumeroPoli, RadiceDwg FROM Interruttore WHERE 1=1"
                    If idMarca.HasValue Then
                        sql &= " AND IdMarca = $idMarca"
                        comando.Parameters.AddWithValue("$idMarca", idMarca.Value)
                    End If
                    If idSerie.HasValue Then
                        sql &= " AND IdSerie = $idSerie"
                        comando.Parameters.AddWithValue("$idSerie", idSerie.Value)
                    End If
                    If numeroPoli.HasValue Then
                        sql &= " AND NumeroPoli = $numeroPoli"
                        comando.Parameters.AddWithValue("$numeroPoli", numeroPoli.Value)
                    End If
                    sql &= " ORDER BY Nome"
                    comando.CommandText = sql

                    Using lettore = comando.ExecuteReader()
                        While lettore.Read()
                            risultato.Add(New Interruttore With {
                                .Id = lettore.GetInt32(0),
                                .IdMarca = lettore.GetInt32(1),
                                .IdSerie = lettore.GetInt32(2),
                                .Nome = lettore.GetString(3),
                                .IdTipo = lettore.GetInt32(4),
                                .NumeroPoli = lettore.GetInt32(5),
                                .RadiceDwg = lettore.GetString(6)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return risultato
        End Function

        ''' <summary>
        ''' Configurazioni applicabili a un interruttore, con i relativi delta H/L/P.
        ''' </summary>
        Public Function ConfigurazioniPerInterruttore(idInterruttore As Integer) As List(Of ViewModels.ConfigurazioneRiga)
            Dim risultato As New List(Of ViewModels.ConfigurazioneRiga)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "
                        SELECT c.Id, c.Configurazione, ci.DeltaH, ci.DeltaL, ci.DeltaP
                        FROM ConfigurazioneInterruttore ci
                        JOIN Configurazione c ON c.Id = ci.IdConfigurazione
                        WHERE ci.IdInterruttore = $id
                        ORDER BY c.Ordine"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Using lettore = comando.ExecuteReader()
                        While lettore.Read()
                            risultato.Add(New ViewModels.ConfigurazioneRiga With {
                                .IdConfigurazione = lettore.GetInt32(0),
                                .Nome = lettore.GetString(1),
                                .DeltaH = If(lettore.IsDBNull(2), CType(Nothing, Double?), lettore.GetDouble(2)),
                                .DeltaL = If(lettore.IsDBNull(3), CType(Nothing, Double?), lettore.GetDouble(3)),
                                .DeltaP = If(lettore.IsDBNull(4), CType(Nothing, Double?), lettore.GetDouble(4))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return risultato
        End Function

        Public Function TaglieDisponibili(idInterruttore As Integer) As List(Of TagliaInterruttore)
            Dim risultato As New List(Of TagliaInterruttore)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT Id, IdInterruttore, Taglia FROM TagliaInterruttore WHERE IdInterruttore = $id ORDER BY Taglia"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Using lettore = comando.ExecuteReader()
                        While lettore.Read()
                            risultato.Add(New TagliaInterruttore With {
                                .Id = lettore.GetInt32(0),
                                .IdInterruttore = lettore.GetInt32(1),
                                .Taglia = lettore.GetString(2)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return risultato
        End Function
    End Class
End Namespace
