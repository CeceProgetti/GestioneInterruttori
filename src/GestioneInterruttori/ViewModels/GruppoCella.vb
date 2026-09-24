Imports System.Linq
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' Un'assegnazione: un insieme di Taglie + un insieme di Linee prodotto a cui vengono dati
    ''' gli stessi valori di cella. Alla conferma, ogni gruppo genera una riga ConfigurazioneCella
    ''' per ogni combinazione Taglia×LineaProdotto al suo interno.
    ''' Nota: LineeProdotto è List(Of LineaProdotto) e non una tupla - i nomi dei campi di una
    ''' tupla VB non sono proprietà reflettibili, quindi il binding WPF "{Binding Nome}" fallirebbe.
    ''' </summary>
    Public Class GruppoCella
        Public Property Taglie As List(Of String)
        Public Property LineeProdotto As List(Of LineaProdotto)

        Public Property AltezzaCella As Double?
        Public Property LarghezzaCella As Double?
        Public Property AltezzaCellaVerticale As Double?
        Public Property LarghezzaCellaVerticale As Double?
        Public Property ProfonditaCella As Double?

        Public ReadOnly Property TaglieDescrizione As String
            Get
                Return String.Join(", ", Taglie)
            End Get
        End Property

        Public ReadOnly Property LineeProdottoDescrizione As String
            Get
                Return String.Join(", ", LineeProdotto.Select(Function(l) l.Nome))
            End Get
        End Property
    End Class
End Namespace
