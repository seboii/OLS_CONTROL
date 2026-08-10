-- transfer_to_siber/loadSave ("Operasyona Bildir") akisinin ihtiyac duydugu
-- Siber nesneleri. Gercek Siber'de bunlar hazir gelir; yerel taklit veritabani
-- icin davranisi taklit eden asgari surumler kuruldu.
USE Siber2022;
GO

-- Denetim kaydi tablosu
IF OBJECT_ID('dbo.sbr_log', 'U') IS NULL
CREATE TABLE dbo.sbr_log (
    kullanici NVARCHAR(128) NULL,
    tablename NVARCHAR(128) NULL,
    tablerecordid NVARCHAR(64) NULL,
    mastertablerecordid NVARCHAR(64) NULL,
    yapilanislem INT NULL,
    findfieldvalue NVARCHAR(255) NULL,
    islemmodul NVARCHAR(255) NULL,
    tarih DATETIME NULL DEFAULT GETDATE());
GO

-- skn_yuk: yuk acma sirasinda doldurulan ek kolonlar
IF COL_LENGTH('skn_yuk', 'rezervasyonid') IS NULL
    ALTER TABLE skn_yuk ADD rezervasyonid NVARCHAR(64) NULL;
GO
IF COL_LENGTH('skn_yuk', 'kredilimitkontroluyapildi') IS NULL
    ALTER TABLE skn_yuk ADD kredilimitkontroluyapildi BIT NULL;
GO

-- Oturum kullanicisi (gercekte Siber'in kendi fonksiyonu)
IF OBJECT_ID('dbo.sbr_program_username', 'FN') IS NOT NULL
    DROP FUNCTION dbo.sbr_program_username;
GO
CREATE FUNCTION dbo.sbr_program_username() RETURNS NVARCHAR(128)
AS
BEGIN
    RETURN SUSER_SNAME();
END
GO

-- Rezervasyon onay kontrolu: gercekte is kurallarini isletir, taklitte
-- her zaman onay verir.
IF OBJECT_ID('dbo.sbr_rezervasyon_onay_kontrol', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sbr_rezervasyon_onay_kontrol;
GO
CREATE PROCEDURE dbo.sbr_rezervasyon_onay_kontrol
    @tip NVARCHAR(1), @rezervasyonid UNIQUEIDENTIFIER, @mesajsor BIT OUTPUT
AS
BEGIN
    SET @mesajsor = 1;
END
GO

-- Yuk acma: rezervasyondan skn_yuk satiri uretir. Ayni rezervasyon icin
-- ikinci kez calistirilirsa yeni satir acmaz (gercekteki davranis).
IF OBJECT_ID('dbo.skn_rezervazyon_yukac', 'P') IS NOT NULL
    DROP PROCEDURE dbo.skn_rezervazyon_yukac;
GO
CREATE PROCEDURE dbo.skn_rezervazyon_yukac @rezervasyonid UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @rez NVARCHAR(64) = CAST(@rezervasyonid AS NVARCHAR(64));

    IF EXISTS (SELECT 1 FROM skn_yuk WHERE rezervasyonid = @rez)
        RETURN;

    DECLARE @yukno INT = ISNULL((SELECT MAX(TRY_CAST(yukno AS INT)) FROM skn_yuk), 0) + 1;

    INSERT INTO skn_yuk
        (yukid, yukno, isturu, durumid, yuklemetip, firmaid, gondericiid, aliciid,
         odemesekliid, talimatgelissekli, istenenromorkcins, departmanid, yukturkod,
         yuknoisturu, rezervasyonid, kredilimitkontroluyapildi)
    SELECT
        LOWER(CONVERT(NVARCHAR(64), NEWID())),
        CAST(@yukno AS NVARCHAR(64)),
        r.isturu, r.durumid, r.yuklemetip, r.musteriid, r.gondericiid, r.aliciid,
        r.odemesekliid, r.talimatgelissekli, r.istenenromorkcins, r.departmanid, r.yukturkod,
        CAST(@yukno AS NVARCHAR(32)) + ISNULL(r.isturu, ''),
        r.rezervasyonid, 1
    FROM skn_rezervasyon r
    WHERE r.rezervasyonid = @rez;
END
GO

-- Tarife aktarimi: rezervasyon tarifelerini yeni yuke tasir.
IF OBJECT_ID('dbo.skn_rezervasyonyukbildir_tarifeaktar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.skn_rezervasyonyukbildir_tarifeaktar;
GO
CREATE PROCEDURE dbo.skn_rezervasyonyukbildir_tarifeaktar
    @rezervasyonid UNIQUEIDENTIFIER, @yukid UNIQUEIDENTIFIER, @kullanici VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    -- Taklit: gercek sistemde sfy_modulkalem satirlari uretilir.
    RETURN;
END
GO

-- Log sorgularinin okudugu gorunumler
IF OBJECT_ID('dbo.skn_rezervasyon_view', 'V') IS NOT NULL
    DROP VIEW dbo.skn_rezervasyon_view;
GO
CREATE VIEW dbo.skn_rezervasyon_view AS
SELECT r.rezervasyonid,
       CAST(r.rezervasyonno AS NVARCHAR(64)) AS rezervasyonno,
       ISNULL((SELECT TOP 1 y.yuknoisturu FROM skn_yuk y
               WHERE y.rezervasyonid = r.rezervasyonid ORDER BY y.yukno DESC), '') AS yuknoisturu
FROM skn_rezervasyon r;
GO

IF OBJECT_ID('dbo.skn_yuk_liste_v2', 'V') IS NOT NULL
    DROP VIEW dbo.skn_yuk_liste_v2;
GO
CREATE VIEW dbo.skn_yuk_liste_v2 AS
SELECT y.yukid, y.yuknoisturu,
       ISNULL((SELECT TOP 1 CAST(r.rezervasyonno AS NVARCHAR(64)) FROM skn_rezervasyon r
               WHERE r.rezervasyonid = y.rezervasyonid), '') AS rezervasyonno
FROM skn_yuk y;
GO

PRINT 'loadSave nesneleri hazir';
GO
