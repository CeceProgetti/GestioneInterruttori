Imports System.ComponentModel
Imports GestioneInterruttori.Models

Namespace ViewModels
    ''' <summary>
    ''' Una opzione del catalogo Configurazione (appartenente a un Tipo, es. Esecuzione/Man_Rot/Diff).
    ''' Ogni opzione ha un flag indipendente: più opzioni dello stesso Tipo possono essere
    ''' abilitate contemporaneamente per lo stesso interruttore, ciascuna coi propri delta H/L/P.
    ''' </summary>
    Public Class ConfigurazioneAssegnabile
        Implements INotifyPropertyChanged

        Public Sub New(configurazione As Configurazione)
            IdConfigurazione = configurazione.Id
            Tipo = configurazione.Tipo
            Nome = configurazione.Configurazione
            EraDefaultDiCatalogo = configurazione.IsDefault
        End Sub

        Public Property IdConfigurazione As Integer
        Public Property Tipo As String
        Public Property Nome As String
        Public Property EraDefaultDiCatalogo As Boolean

        Private _abilitata As Boolean
        Public Property Abilitata As Boolean
            Get
                Return _abilitata
            End Get
            Set(value As Boolean)
                _abilitata = value
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(Abilitata)))
            End Set
        End Property

        Public Property DeltaH As Double?
        Public Property DeltaL As Double?
        Public Property DeltaP As Double?

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    End Class
End Namespace
