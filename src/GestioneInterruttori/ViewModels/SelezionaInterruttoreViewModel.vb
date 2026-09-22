Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports GestioneInterruttori.Data
Imports GestioneInterruttori.Models

Namespace ViewModels
    Public Class SelezionaInterruttoreViewModel
        Implements INotifyPropertyChanged

        Private ReadOnly _repository As InterruttoreRepository

        Public Sub New(repository As InterruttoreRepository)
            _repository = repository
            Marche = New ObservableCollection(Of Marca)(_repository.ElencoMarche())
            Serie = New ObservableCollection(Of Serie)(_repository.ElencoSerie())
            Interruttori = New ObservableCollection(Of Interruttore)
            Configurazioni = New ObservableCollection(Of ConfigurazioneRiga)

            CercaCommand = New RelayCommand(AddressOf EseguiRicerca)
            ConfermaCommand = New RelayCommand(AddressOf EseguiConferma, Function() InterruttoreSelezionato IsNot Nothing)

            EseguiRicerca()
        End Sub

        Public ReadOnly Property Marche As ObservableCollection(Of Marca)
        Public ReadOnly Property Serie As ObservableCollection(Of Serie)
        Public ReadOnly Property Interruttori As ObservableCollection(Of Interruttore)
        Public ReadOnly Property Configurazioni As ObservableCollection(Of ConfigurazioneRiga)

        Public ReadOnly Property NumeriPoli As Integer() = {1, 2, 3, 4}

        Private _marcaSelezionata As Marca
        Public Property MarcaSelezionata As Marca
            Get
                Return _marcaSelezionata
            End Get
            Set(value As Marca)
                _marcaSelezionata = value
                OnPropertyChanged()
                EseguiRicerca()
            End Set
        End Property

        Private _serieSelezionata As Serie
        Public Property SerieSelezionata As Serie
            Get
                Return _serieSelezionata
            End Get
            Set(value As Serie)
                _serieSelezionata = value
                OnPropertyChanged()
                EseguiRicerca()
            End Set
        End Property

        Private _numeroPoliSelezionato As Integer?
        Public Property NumeroPoliSelezionato As Integer?
            Get
                Return _numeroPoliSelezionato
            End Get
            Set(value As Integer?)
                _numeroPoliSelezionato = value
                OnPropertyChanged()
                EseguiRicerca()
            End Set
        End Property

        Private _interruttoreSelezionato As Interruttore
        Public Property InterruttoreSelezionato As Interruttore
            Get
                Return _interruttoreSelezionato
            End Get
            Set(value As Interruttore)
                _interruttoreSelezionato = value
                OnPropertyChanged()
                CaricaConfigurazioni()
                DirectCast(ConfermaCommand, RelayCommand).RaiseCanExecuteChanged()
            End Set
        End Property

        Public ReadOnly Property CercaCommand As RelayCommand
        Public ReadOnly Property ConfermaCommand As RelayCommand

        ''' <summary>Esito della maschera: valorizzato solo se l'utente conferma.</summary>
        Public Property RisultatoConfermato As Boolean

        Private Sub EseguiRicerca()
            Interruttori.Clear()
            Dim trovati = _repository.CercaInterruttori(
                idMarca:=If(MarcaSelezionata Is Nothing, CType(Nothing, Integer?), MarcaSelezionata.Id),
                idSerie:=If(SerieSelezionata Is Nothing, CType(Nothing, Integer?), SerieSelezionata.Id),
                numeroPoli:=NumeroPoliSelezionato)
            For Each i In trovati
                Interruttori.Add(i)
            Next
        End Sub

        Private Sub CaricaConfigurazioni()
            Configurazioni.Clear()
            If InterruttoreSelezionato Is Nothing Then Return
            For Each c In _repository.ConfigurazioniPerInterruttore(InterruttoreSelezionato.Id)
                Configurazioni.Add(c)
            Next
        End Sub

        Private Sub EseguiConferma()
            RisultatoConfermato = True
            RaiseEvent RichiestaChiusura(Me, EventArgs.Empty)
        End Sub

        Public Event RichiestaChiusura As EventHandler

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private Sub OnPropertyChanged(<CallerMemberName> Optional nomeProprieta As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(nomeProprieta))
        End Sub
    End Class
End Namespace
