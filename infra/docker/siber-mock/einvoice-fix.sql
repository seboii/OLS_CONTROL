-- Siber e-fatura tablolarini gercek ETL'in okudugu sutunlarla tamamlar.
--
-- init.sql bu tablolarin ucunu yalnizca anahtar sutunla olusturmustu;
-- pull_sfy_* uclari portlanirken eksik sutunlar eklendi. Sutun adlari
-- olsold'un TransferDataController kullanimindan cikarildi.
USE Siber2022;
GO

IF COL_LENGTH('sfy_edurum','kod') IS NULL
ALTER TABLE sfy_edurum ADD
    kod                              INT            NULL,
    ad                               NVARCHAR(200)  NULL,
    bimsa_kod                        INT            NULL,
    hata                             INT            NULL,
    ticarifatura_son                 INT            NULL,
    temelfatura_son                  INT            NULL,
    efaturakullanim                  INT            NULL,
    digplan_kod                      INT            NULL,
    tekrargonderilebilir             INT            NULL,
    tekrargondermedeyenizarfidkullan INT            NULL,
    efinans_kod                      INT            NULL,
    edm_kod                          NVARCHAR(50)   NULL,
    vbt_kod                          NVARCHAR(50)   NULL,
    uyumsoft_kod                     NVARCHAR(50)   NULL;
GO

IF COL_LENGTH('sfy_efirma','firmaid') IS NULL
ALTER TABLE sfy_efirma ADD
    firmaid            NVARCHAR(50)   NULL,
    firmaad            NVARCHAR(300)  NULL,
    sokak              NVARCHAR(300)  NULL,
    binaad             NVARCHAR(200)  NULL,
    kapino             NVARCHAR(50)   NULL,
    ilcesemt           NVARCHAR(200)  NULL,
    il                 NVARCHAR(200)  NULL,
    ulke               NVARCHAR(200)  NULL,
    ulkpostakod        NVARCHAR(50)   NULL,
    vergidaire         NVARCHAR(200)  NULL,
    vergino_tckimlikno NVARCHAR(50)   NULL,
    webadres           NVARCHAR(300)  NULL,
    eposta             NVARCHAR(300)  NULL,
    telefon            NVARCHAR(50)   NULL,
    fax                NVARCHAR(50)   NULL;
GO

-- sfy_efatura kismen doluydu; yalnizca eksik sutunlar eklenir.
IF COL_LENGTH('sfy_efatura','aciklama') IS NULL
ALTER TABLE sfy_efatura ADD
    aciklama              NVARCHAR(500)  NULL,
    gelirgiderid          NVARCHAR(50)   NULL,
    faturacevapdurumkod   INT            NULL,
    kdvtevkifattutar      DECIMAL(18,4)  NULL,
    modulid               NVARCHAR(50)   NULL,
    modulad               NVARCHAR(200)  NULL,
    gonderimefaturano     NVARCHAR(100)  NULL,
    gonderimefaturatarih  DATETIME       NULL,
    gonderimefaturatsaat  NVARCHAR(20)   NULL,
    goruldu               INT            NULL,
    manuel_olustu         INT            NULL,
    sepettarih            DATETIME       NULL,
    uuid                  NVARCHAR(100)  NULL,
    romorkplakano         NVARCHAR(50)   NULL;
GO

IF COL_LENGTH('sfy_efaturadetay','efaturaid') IS NULL
ALTER TABLE sfy_efaturadetay ADD
    efaturaid              NVARCHAR(50)   NULL,
    stokad                 NVARCHAR(300)  NULL,
    aciklama               NVARCHAR(500)  NULL,
    miktar                 DECIMAL(18,4)  NULL,
    birim                  NVARCHAR(50)   NULL,
    doviztip               NVARCHAR(10)   NULL,
    birimfiyat             DECIMAL(18,4)  NULL,
    toplamtutar            DECIMAL(18,4)  NULL,
    iskontooran            INT            NULL,
    iskontotutar           DECIMAL(18,4)  NULL,
    kdv_oran               INT            NULL,
    kdv_tutar              DECIMAL(18,4)  NULL,
    instime                DATETIME       NULL,
    insuser                NVARCHAR(50)   NULL,
    gelirgiderdetayid      NVARCHAR(50)   NULL,
    modulid                NVARCHAR(50)   NULL,
    efaturadetayportalsira INT            NULL,
    vergitutar             DECIMAL(18,4)  NULL,
    kdv_muafiyetkod        INT            NULL,
    orjtutar               DECIMAL(18,4)  NULL,
    orjdovizkod            NVARCHAR(10)   NULL;
GO
