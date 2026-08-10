-- sbr_kzgelirgider tablosunu gercek ETL'in okudugu sutunlarla tamamlar.
--
-- init.sql bu tabloyu yalnizca anahtar sutunla olusturuyordu; gelir/gider
-- ETL'i (get:PullSbrKzGelirGider) portlanirken eksik sutunlar eklendi.
-- Sutun adlari olsold'un PullSbrKzGelirGider komutundaki kullanimdan cikarildi.
USE Siber2022;
GO

IF COL_LENGTH('sbr_kzgelirgider','sektorad') IS NULL
ALTER TABLE sbr_kzgelirgider ADD
    sektorad              NVARCHAR(200)  NULL,
    pozisyonno            NVARCHAR(50)   NULL,
    pozisyonid            NVARCHAR(50)   NULL,
    yukno                 NVARCHAR(50)   NULL,
    yukid                 NVARCHAR(50)   NULL,
    belgetarih            DATETIME       NULL,
    kalemid               NVARCHAR(50)   NULL,
    firmaid               NVARCHAR(50)   NULL,

    BeklenenGelirTL       DECIMAL(18,4)  NULL,
    BeklenenGiderTL       DECIMAL(18,4)  NULL,
    GerceklesenGelirTL    DECIMAL(18,4)  NULL,
    GerceklesenGiderTL    DECIMAL(18,4)  NULL,
    BeklenenGelirEUR      DECIMAL(18,4)  NULL,
    BeklenenGiderEUR      DECIMAL(18,4)  NULL,
    GerceklesenGelirEUR   DECIMAL(18,4)  NULL,
    GerceklesenGiderEUR   DECIMAL(18,4)  NULL,
    BeklenenGelirUSD      DECIMAL(18,4)  NULL,
    BeklenenGiderUSD      DECIMAL(18,4)  NULL,
    GerceklesenGelirUSD   DECIMAL(18,4)  NULL,
    GerceklesenGiderUSD   DECIMAL(18,4)  NULL,
    BeklenenGelirGBP      DECIMAL(18,4)  NULL,
    BeklenenGiderGBP      DECIMAL(18,4)  NULL,
    GerceklesenGelirGBP   DECIMAL(18,4)  NULL,
    GerceklesenGiderGBP   DECIMAL(18,4)  NULL,
    BeklenenGelirORJ      DECIMAL(18,4)  NULL,
    BeklenenGiderORJ      DECIMAL(18,4)  NULL,
    GerceklesenGelirORJ   DECIMAL(18,4)  NULL,
    GerceklesenGiderORJ   DECIMAL(18,4)  NULL,

    islem                 NVARCHAR(100)  NULL,
    belgeno               NVARCHAR(100)  NULL,
    toplamagirlik         DECIMAL(18,4)  NULL,
    toplamhacim           DECIMAL(18,4)  NULL,
    ucretagirlik          DECIMAL(18,4)  NULL,
    ciroTL                DECIMAL(18,4)  NULL,
    navlunciroTL          DECIMAL(18,4)  NULL,
    yuksayisi             INT            NULL,
    tutarorjinal          DECIMAL(18,4)  NULL,
    orjnaldovizkod        NVARCHAR(10)   NULL,
    dovizkur              DECIMAL(18,6)  NULL,
    tip                   NVARCHAR(50)   NULL,
    masterkeyid           NVARCHAR(50)   NULL,
    tedarikciid           NVARCHAR(50)   NULL,
    satistemsilcisi       NVARCHAR(50)   NULL,
    musteriid             NVARCHAR(50)   NULL,
    aciklama              NVARCHAR(500)  NULL;
GO
