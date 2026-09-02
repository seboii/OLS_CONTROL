-- Sahte Siber: içe aktarımın OKUDUĞU referans tabloları.
--
-- init.sql bu tabloları yalnızca (id, ad, kod) iskeletiyle oluşturuyordu; çünkü
-- olsold bu tablolardan sadece "SELECT *" yapıp alanlara PHP tarafında dinamik
-- erişiyor, dolayısıyla kolon adları kaynak koddan statik olarak çıkarılamıyordu.
--
-- Aşağıdaki kolonlar ETL'in gerçekten okuduğu alanlardır
-- (bkz. OLS.Business/Services/TransferData/SiberImportService.cs).
USE Siber2022;
GO

-- İskelet tabloları düşürüp doğru kolonlarla yeniden oluşturuyoruz.
DROP TABLE IF EXISTS sbr_ulke, sbr_sehir, sbr_ilce, sbr_vergidaire,
    sdn_rezervasyondurum, sbr_odemesekli, sbr_departman, sbr_malcinsi,
    skn_kapcins, skn_kalem, sbr_doviztur, sbr_dovizkur, sky_kullanici,
    skn_yukdurum, sbr_teslimsekli, skn_pozisyondurum, sfy_muhasebeentegrekodu;
GO

-- kita: gercek Siber'de tinyint ve skn_sabittanim'in KITA grubuna karsilik
-- gelir (0 AFRIKA, 1 ASYA, 2 AVRUPA, 3 AMERIKA, 4 AVUSTURALYA). Yuk kaydinda
-- _yuklemekita/_bosaltmakita bu tablodan turer.
CREATE TABLE sbr_ulke (
    ulkeid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), telefonkod NVARCHAR(16),
    kisaad NVARCHAR(16), kita TINYINT NULL);

CREATE TABLE sbr_sehir (
    sehirid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), ulkeid NVARCHAR(64));

CREATE TABLE sbr_ilce (
    ilceid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), sehirid NVARCHAR(64));

CREATE TABLE sbr_vergidaire (
    vergidaireid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), ozelkod INT, sehir NVARCHAR(255));

CREATE TABLE sdn_rezervasyondurum (
    durumid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), sirano NVARCHAR(32));

CREATE TABLE sbr_odemesekli (
    odemesekliid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), kodu NVARCHAR(64));

CREATE TABLE sbr_departman (
    departmanid NVARCHAR(64) NOT NULL, ad NVARCHAR(255));

CREATE TABLE sbr_malcinsi (
    malcinsid NVARCHAR(64) NOT NULL, ad NVARCHAR(255));

CREATE TABLE skn_kapcins (
    kapcinsid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), edikod NVARCHAR(64));

CREATE TABLE skn_kalem (
    kalemid NVARCHAR(64) NOT NULL, ad NVARCHAR(255));

CREATE TABLE sbr_doviztur (
    rowguid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), kod NVARCHAR(16));

CREATE TABLE sbr_dovizkur (
    tarih DATETIME, dovizkod NVARCHAR(16),
    dovizalis DECIMAL(18,6), dovizsatis DECIMAL(18,6),
    efektifalis DECIMAL(18,6), efektifsatis DECIMAL(18,6));

-- pass: gercek Siber'de varbinary(255) ve SQL Server'in kendi sifre ozeti
-- (PWDENCRYPT). Dogrulama PWDCOMPARE(UPPER(@sifre), pass) ile sunucuda yapilir
-- (bkz. ISiberUserRepository) -- ozet geri cevrilemez, uygulama sifreyi gormez.
CREATE TABLE sky_kullanici (
    kullaniciid NVARCHAR(64) NOT NULL, ad NVARCHAR(255), kod NVARCHAR(64),
    email NVARCHAR(255), engelle INT, pass VARBINARY(255) NULL);

CREATE TABLE skn_yukdurum (
    yukdurumid INT, ad NVARCHAR(255), sirano INT);

CREATE TABLE sbr_teslimsekli (
    teslimsekliid NVARCHAR(64) NOT NULL, edikod NVARCHAR(64), ad NVARCHAR(255));

CREATE TABLE skn_pozisyondurum (
    pozisyondurumid INT, ad NVARCHAR(255), yukdurumid INT,
    rowguid NVARCHAR(64), sirano INT);

-- sbr_firma ile ad üzerinden birleşen muhasebe kodu tablosu
CREATE TABLE sfy_muhasebeentegrekodu (
    entegread NVARCHAR(255), muhasebekod NVARCHAR(32));
GO

-- skn_sabittanim: init.sql'de yalnızca grupkod vardı; ETL ad/kod/ozelkod/ekkod
-- ve sabittanimid alanlarını da okuyor.
DROP TABLE IF EXISTS skn_sabittanim;
CREATE TABLE skn_sabittanim (
    sabittanimid NVARCHAR(64) NOT NULL, grupkod NVARCHAR(64),
    ad NVARCHAR(255), kod INT, ozelkod INT, ekkod NVARCHAR(64));
GO

PRINT 'referans tablolari hazir';
GO
