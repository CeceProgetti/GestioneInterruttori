CREATE TABLE Marca (
    Id   INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL UNIQUE
);

CREATE TABLE Serie (
    Id   INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL UNIQUE
);

CREATE TABLE TipoInterruttore (
    Id     INTEGER PRIMARY KEY,
    Tipo   TEXT NOT NULL,
    XCella TEXT,
    YCella TEXT
);

-- Anagrafica base dell'interruttore
CREATE TABLE Interruttore (
    Id          INTEGER PRIMARY KEY,
    IdMarca     INTEGER NOT NULL REFERENCES Marca(Id),
    IdSerie     INTEGER NOT NULL REFERENCES Serie(Id),
    Nome        TEXT NOT NULL,
    IdTipo      INTEGER NOT NULL REFERENCES TipoInterruttore(Id),
    NumeroPoli  INTEGER NOT NULL,
    RadiceDwg   TEXT NOT NULL       -- ex DWG_INTERRUTTORE
);

-- Taglie disponibili per l'interruttore
CREATE TABLE TagliaInterruttore (
    Id               INTEGER PRIMARY KEY,
    IdInterruttore   INTEGER NOT NULL REFERENCES Interruttore(Id),
    Taglia           TEXT NOT NULL,
    PotenzaDissipata REAL,
    UNIQUE(IdInterruttore, Taglia)
);

-- Catalogo di tutte le configurazioni possibili, raggruppate per Tipo
-- (es. Tipo 'Esecuzione' -> opzioni Fissa/Esterna/Rem, una delle quali di Default)
CREATE TABLE Configurazione (
    Id             INTEGER PRIMARY KEY,
    Tipo           TEXT NOT NULL,   -- es. 'Esecuzione', 'Man_Rot', 'Diff'
    Ordine         INTEGER NOT NULL, -- posizione del Tipo nella concatenazione del nome blocco
    Configurazione TEXT NOT NULL,   -- es. 'Fissa', 'Esterna', 'Rem'
    DesinenzaDwg   TEXT NOT NULL,
    "Default"      INTEGER NOT NULL DEFAULT 0  -- 1 = opzione di default per quel Tipo
);

-- Configurazioni applicabili a quel interruttore + delta
CREATE TABLE ConfigurazioneInterruttore (
    IdInterruttore   INTEGER NOT NULL REFERENCES Interruttore(Id),
    IdConfigurazione INTEGER NOT NULL REFERENCES Configurazione(Id),
    DeltaH           REAL,
    DeltaL           REAL,
    DeltaP           REAL,
    PRIMARY KEY (IdInterruttore, IdConfigurazione)
);

-- Linee prodotto: NORMA30 / NORMA40 / NORMA50
CREATE TABLE LineaProdotto (
    Id   INTEGER PRIMARY KEY,
    Nome TEXT NOT NULL UNIQUE
);

-- Dimensioni cella per linea + interruttore + taglia
CREATE TABLE ConfigurazioneCella (
    IdLineaProdotto         INTEGER NOT NULL REFERENCES LineaProdotto(Id),
    IdInterruttore          INTEGER NOT NULL,
    Taglia                  TEXT NOT NULL,
    AltezzaCella             REAL,
    LarghezzaCella           REAL,
    AltezzaCellaVerticale    REAL,
    LarghezzaCellaVerticale  REAL,
    ProfonditaCella          REAL,   -- ex PROFONDITA
    PRIMARY KEY (IdLineaProdotto, IdInterruttore, Taglia),
    FOREIGN KEY (IdInterruttore, Taglia) REFERENCES TagliaInterruttore(IdInterruttore, Taglia)
);
