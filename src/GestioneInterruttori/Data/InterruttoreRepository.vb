Imports Microsoft.Data.Sqlite
Imports GestioneInterruttori.Models
Imports System.Linq

Namespace Data
    ''' <summary>
    ''' Accesso dati per la maschera di gestione anagrafica interruttori (inserimento/modifica).
    ''' </summary>
    Public Class InterruttoreRepository
        Private ReadOnly _db As InterruttoriDbContext

        Public Sub New(db As InterruttoriDbContext)
            _db = db
        End Sub

#Region "Elenchi di supporto (combobox, cataloghi)"

        Public Function ElencoMarche() As List(Of Marca)
            Return LeggiElenco("SELECT Id, Nome FROM Marca ORDER BY Nome",
                Function(r) New Marca With {.Id = r.GetInt32(0), .Nome = r.GetString(1)})
        End Function

        Public Function ElencoSerie() As List(Of Serie)
            Return LeggiElenco("SELECT Id, Nome FROM Serie ORDER BY Nome",
                Function(r) New Serie With {.Id = r.GetInt32(0), .Nome = r.GetString(1)})
        End Function

        Public Function ElencoTipi() As List(Of TipoInterruttore)
            Return LeggiElenco("SELECT Id, Tipo, XCella, YCella FROM TipoInterruttore ORDER BY Tipo",
                Function(r) New TipoInterruttore With {
                    .Id = r.GetInt32(0), .Tipo = r.GetString(1),
                    .XCella = If(r.IsDBNull(2), Nothing, r.GetString(2)),
                    .YCella = If(r.IsDBNull(3), Nothing, r.GetString(3))
                })
        End Function

        Public Function ElencoLineeProdotto() As List(Of LineaProdotto)
            Return LeggiElenco("SELECT Id, Nome FROM LineaProdotto ORDER BY Nome",
                Function(r) New LineaProdotto With {.Id = r.GetInt32(0), .Nome = r.GetString(1)})
        End Function

        Public Function ElencoConfigurazioniCatalogo() As List(Of Configurazione)
            Return LeggiElenco("SELECT Id, Tipo, Ordine, Configurazione, DesinenzaDwg, ""Default"" FROM Configurazione ORDER BY Ordine, Id",
                Function(r) New Configurazione With {
                    .Id = r.GetInt32(0),
                    .Tipo = r.GetString(1),
                    .Ordine = r.GetInt32(2),
                    .Configurazione = r.GetString(3),
                    .DesinenzaDwg = r.GetString(4),
                    .IsDefault = r.GetInt32(5) <> 0
                })
        End Function

#End Region

#Region "Elenco e dettaglio interruttori"

        Public Function ElencoInterruttori() As List(Of Interruttore)
            Return LeggiElenco("SELECT Id, IdMarca, IdSerie, Nome, IdTipo, NumeroPoli, RadiceDwg FROM Interruttore ORDER BY Nome",
                Function(r) LeggiInterruttore(r))
        End Function

        Public Function OttieniInterruttore(idInterruttore As Integer) As Interruttore
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT Id, IdMarca, IdSerie, Nome, IdTipo, NumeroPoli, RadiceDwg FROM Interruttore WHERE Id = $id"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Return LeggiRighe(comando, Function(r) LeggiInterruttore(r)).FirstOrDefault()
                End Using
            End Using
        End Function

        Public Function TaglieDiInterruttore(idInterruttore As Integer) As List(Of TagliaInterruttore)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT Id, IdInterruttore, Taglia, PotenzaDissipata FROM TagliaInterruttore WHERE IdInterruttore = $id ORDER BY Taglia"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Return LeggiRighe(comando, Function(r) New TagliaInterruttore With {
                        .Id = r.GetInt32(0), .IdInterruttore = r.GetInt32(1), .Taglia = r.GetString(2),
                        .PotenzaDissipata = LeggiDoubleNullable(r, 3)
                    })
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Configurazioni assegnate a un interruttore, con nome Tipo/opzione già risolti
        ''' per la visualizzazione in sola lettura nella maschera principale.
        ''' </summary>
        Public Function ConfigurazioniAssegnateDescrizione(idInterruttore As Integer) As List(Of (Tipo As String, Nome As String, DeltaH As Double?, DeltaL As Double?, DeltaP As Double?))
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "
                        SELECT c.Tipo, c.Configurazione, ci.DeltaH, ci.DeltaL, ci.DeltaP
                        FROM ConfigurazioneInterruttore ci
                        JOIN Configurazione c ON c.Id = ci.IdConfigurazione
                        WHERE ci.IdInterruttore = $id
                        ORDER BY c.Ordine, c.Id"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Return LeggiRighe(comando, Function(r) (
                        Tipo:=r.GetString(0),
                        Nome:=r.GetString(1),
                        DeltaH:=LeggiDoubleNullable(r, 2),
                        DeltaL:=LeggiDoubleNullable(r, 3),
                        DeltaP:=LeggiDoubleNullable(r, 4)))
                End Using
            End Using
        End Function

        Public Function ConfigurazioniDiInterruttore(idInterruttore As Integer) As List(Of ConfigurazioneInterruttore)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT IdInterruttore, IdConfigurazione, DeltaH, DeltaL, DeltaP FROM ConfigurazioneInterruttore WHERE IdInterruttore = $id"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Return LeggiRighe(comando, Function(r) New ConfigurazioneInterruttore With {
                        .IdInterruttore = r.GetInt32(0), .IdConfigurazione = r.GetInt32(1),
                        .DeltaH = LeggiDoubleNullable(r, 2), .DeltaL = LeggiDoubleNullable(r, 3), .DeltaP = LeggiDoubleNullable(r, 4)
                    })
                End Using
            End Using
        End Function

        Public Function CelleDiInterruttore(idInterruttore As Integer) As List(Of ConfigurazioneCella)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = "SELECT IdLineaProdotto, IdInterruttore, Taglia, AltezzaCella, LarghezzaCella, AltezzaCellaVerticale, LarghezzaCellaVerticale, ProfonditaCella FROM ConfigurazioneCella WHERE IdInterruttore = $id"
                    comando.Parameters.AddWithValue("$id", idInterruttore)
                    Return LeggiRighe(comando, Function(r) New ConfigurazioneCella With {
                        .IdLineaProdotto = r.GetInt32(0), .IdInterruttore = r.GetInt32(1), .Taglia = r.GetString(2),
                        .AltezzaCella = LeggiDoubleNullable(r, 3), .LarghezzaCella = LeggiDoubleNullable(r, 4),
                        .AltezzaCellaVerticale = LeggiDoubleNullable(r, 5), .LarghezzaCellaVerticale = LeggiDoubleNullable(r, 6),
                        .ProfonditaCella = LeggiDoubleNullable(r, 7)
                    })
                End Using
            End Using
        End Function

#End Region

#Region "Salvataggio"

        ''' <summary>
        ''' Salva anagrafica + taglie in un'unica transazione (le taglie vengono sostituite
        ''' integralmente: delete + insert). Configurazioni e Celle hanno un salvataggio a parte,
        ''' dalle rispettive maschere dedicate (SalvaConfigurazioni, e in futuro SalvaCelle).
        ''' </summary>
        Public Function Salva(interruttore As Interruttore,
                               taglie As IEnumerable(Of TagliaInterruttore)) As Integer
            Using connessione = _db.CreaConnessione()
                Using transazione = connessione.BeginTransaction()

                    Dim idInterruttore As Integer = interruttore.Id
                    If idInterruttore = 0 Then
                        Using comando = connessione.CreateCommand()
                            comando.Transaction = transazione
                            comando.CommandText = "
                                INSERT INTO Interruttore (IdMarca, IdSerie, Nome, IdTipo, NumeroPoli, RadiceDwg)
                                VALUES ($idMarca, $idSerie, $nome, $idTipo, $poli, $radiceDwg);
                                SELECT last_insert_rowid();"
                            AggiungiParametriInterruttore(comando, interruttore)
                            idInterruttore = CInt(CLng(comando.ExecuteScalar()))
                        End Using
                    Else
                        Using comando = connessione.CreateCommand()
                            comando.Transaction = transazione
                            comando.CommandText = "
                                UPDATE Interruttore
                                SET IdMarca = $idMarca, IdSerie = $idSerie, Nome = $nome,
                                    IdTipo = $idTipo, NumeroPoli = $poli, RadiceDwg = $radiceDwg
                                WHERE Id = $id"
                            AggiungiParametriInterruttore(comando, interruttore)
                            comando.Parameters.AddWithValue("$id", idInterruttore)
                            comando.ExecuteNonQuery()
                        End Using
                    End If

                    RigenerraFigli(connessione, transazione, "TagliaInterruttore", "IdInterruttore", idInterruttore)
                    For Each taglia In taglie
                        Using comando = connessione.CreateCommand()
                            comando.Transaction = transazione
                            comando.CommandText = "INSERT INTO TagliaInterruttore (IdInterruttore, Taglia, PotenzaDissipata) VALUES ($id, $taglia, $potenza)"
                            comando.Parameters.AddWithValue("$id", idInterruttore)
                            comando.Parameters.AddWithValue("$taglia", taglia.Taglia)
                            comando.Parameters.AddWithValue("$potenza", AValoreONullo(taglia.PotenzaDissipata))
                            comando.ExecuteNonQuery()
                        End Using
                    Next

                    transazione.Commit()
                    Return idInterruttore
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Salva solo la sezione Configurazioni di un interruttore già esistente
        ''' (utile per testare/lavorare su questa sola parte della maschera).
        ''' </summary>
        Public Sub SalvaConfigurazioni(idInterruttore As Integer, configurazioni As IEnumerable(Of ConfigurazioneInterruttore))
            Using connessione = _db.CreaConnessione()
                Using transazione = connessione.BeginTransaction()
                    RigenerraFigli(connessione, transazione, "ConfigurazioneInterruttore", "IdInterruttore", idInterruttore)
                    For Each c In configurazioni
                        Using comando = connessione.CreateCommand()
                            comando.Transaction = transazione
                            comando.CommandText = "
                                INSERT INTO ConfigurazioneInterruttore (IdInterruttore, IdConfigurazione, DeltaH, DeltaL, DeltaP)
                                VALUES ($id, $idConfig, $deltaH, $deltaL, $deltaP)"
                            comando.Parameters.AddWithValue("$id", idInterruttore)
                            comando.Parameters.AddWithValue("$idConfig", c.IdConfigurazione)
                            comando.Parameters.AddWithValue("$deltaH", AValoreONullo(c.DeltaH))
                            comando.Parameters.AddWithValue("$deltaL", AValoreONullo(c.DeltaL))
                            comando.Parameters.AddWithValue("$deltaP", AValoreONullo(c.DeltaP))
                            comando.ExecuteNonQuery()
                        End Using
                    Next
                    transazione.Commit()
                End Using
            End Using
        End Sub

        Public Sub Elimina(idInterruttore As Integer)
            Using connessione = _db.CreaConnessione()
                Using transazione = connessione.BeginTransaction()
                    RigenerraFigli(connessione, transazione, "TagliaInterruttore", "IdInterruttore", idInterruttore)
                    RigenerraFigli(connessione, transazione, "ConfigurazioneInterruttore", "IdInterruttore", idInterruttore)
                    RigenerraFigli(connessione, transazione, "ConfigurazioneCella", "IdInterruttore", idInterruttore)
                    Using comando = connessione.CreateCommand()
                        comando.Transaction = transazione
                        comando.CommandText = "DELETE FROM Interruttore WHERE Id = $id"
                        comando.Parameters.AddWithValue("$id", idInterruttore)
                        comando.ExecuteNonQuery()
                    End Using
                    transazione.Commit()
                End Using
            End Using
        End Sub

#End Region

#Region "Helper privati"

        Private Sub AggiungiParametriInterruttore(comando As SqliteCommand, interruttore As Interruttore)
            comando.Parameters.AddWithValue("$idMarca", interruttore.IdMarca)
            comando.Parameters.AddWithValue("$idSerie", interruttore.IdSerie)
            comando.Parameters.AddWithValue("$nome", If(interruttore.Nome, ""))
            comando.Parameters.AddWithValue("$idTipo", interruttore.IdTipo)
            comando.Parameters.AddWithValue("$poli", interruttore.NumeroPoli)
            comando.Parameters.AddWithValue("$radiceDwg", If(interruttore.RadiceDwg, ""))
        End Sub

        Private Sub RigenerraFigli(connessione As SqliteConnection, transazione As SqliteTransaction, tabella As String, colonnaFk As String, idInterruttore As Integer)
            Using comando = connessione.CreateCommand()
                comando.Transaction = transazione
                comando.CommandText = $"DELETE FROM {tabella} WHERE {colonnaFk} = $id"
                comando.Parameters.AddWithValue("$id", idInterruttore)
                comando.ExecuteNonQuery()
            End Using
        End Sub

        Private Function LeggiInterruttore(r As SqliteDataReader) As Interruttore
            Return New Interruttore With {
                .Id = r.GetInt32(0), .IdMarca = r.GetInt32(1), .IdSerie = r.GetInt32(2),
                .Nome = r.GetString(3), .IdTipo = r.GetInt32(4), .NumeroPoli = r.GetInt32(5), .RadiceDwg = r.GetString(6)
            }
        End Function

        Private Function LeggiDoubleNullable(r As SqliteDataReader, indice As Integer) As Double?
            Return If(r.IsDBNull(indice), CType(Nothing, Double?), r.GetDouble(indice))
        End Function

        ''' <summary>
        ''' Converte un Double? in un valore da passare a un parametro SQLite (DBNull.Value se assente).
        ''' Nota: da chiamare come funzione normale, MAI come "valore.AValoreONullo()" - un'eventuale
        ''' chiamata in stile extension su un'espressione tipizzata Object farebbe scattare il late
        ''' binding di VB, che lancia NullReferenceException quando il valore è Nothing.
        ''' </summary>
        Private Function AValoreONullo(valore As Double?) As Object
            If valore.HasValue Then
                Return valore.Value
            End If
            Return DBNull.Value
        End Function

        Private Function LeggiElenco(Of T)(sql As String, mappa As Func(Of SqliteDataReader, T)) As List(Of T)
            Using connessione = _db.CreaConnessione()
                Using comando = connessione.CreateCommand()
                    comando.CommandText = sql
                    Return LeggiRighe(comando, mappa)
                End Using
            End Using
        End Function

        Private Function LeggiRighe(Of T)(comando As SqliteCommand, mappa As Func(Of SqliteDataReader, T)) As List(Of T)
            Dim risultato As New List(Of T)
            Using lettore = comando.ExecuteReader()
                While lettore.Read()
                    risultato.Add(mappa(lettore))
                End While
            End Using
            Return risultato
        End Function

#End Region
    End Class
End Namespace
