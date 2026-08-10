-- expedition_load_mapping akışının kullandığı kolonlar.
-- sample-movements.sql'in kurduğu skn_yuk'ta pozisyonid yok; yük bir sefere
-- bağlandığında bu alan güncelleniyor. skn_yukaktarma'yı da ETL'in okuduğu
-- kolonlarla yeniden kuruyoruz (init.sql sürümü yuklemebosaltma'yı metin
-- tutuyor ve id alanı taşımıyor).
USE Siber2022;
GO

IF COL_LENGTH('skn_yuk', 'pozisyonid') IS NULL
    ALTER TABLE skn_yuk ADD pozisyonid NVARCHAR(64) NULL;
GO

IF OBJECT_ID('dbo.skn_yukaktarma', 'U') IS NOT NULL
    DROP TABLE dbo.skn_yukaktarma;
GO

CREATE TABLE skn_yukaktarma (
    yukaktarmaid NVARCHAR(64),
    yuklemebosaltma INT,
    yukid NVARCHAR(64),
    pozisyonid NVARCHAR(64),
    romorkid NVARCHAR(64),
    yerid NVARCHAR(64),
    tarih DATETIME);
GO
