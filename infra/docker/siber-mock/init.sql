-- Yerel sahte Siber (MSSQL). ELLE DUZENLEMEYIN - gen_siber_sql.php uretir.
-- Kolonlar olsold kaynak kodundaki kullanimdan cikarildi; gercek Siber2022
-- semasinin birebir kopyasi DEGILDIR. Amac: canliya dokunmadan akis testi.

IF DB_ID('Siber2022') IS NULL CREATE DATABASE Siber2022;
GO
USE Siber2022;
GO

IF OBJECT_ID('dbo.sbr_departman', 'U') IS NULL
CREATE TABLE dbo.[sbr_departman] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_dovizkur', 'U') IS NULL
CREATE TABLE dbo.[sbr_dovizkur] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_doviztur', 'U') IS NULL
CREATE TABLE dbo.[sbr_doviztur] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_firma', 'U') IS NULL
CREATE TABLE dbo.[sbr_firma] (
    [account_type_id] NVARCHAR(64) NULL,
    [accounting_code] NVARCHAR(255) NULL,
    [ad] NVARCHAR(255) NULL,
    [address] NVARCHAR(255) NULL,
    [adres1] NVARCHAR(255) NULL,
    [aktif] NVARCHAR(255) NULL,
    [alici] NVARCHAR(255) NULL,
    [antrepomusteri] NVARCHAR(255) NULL,
    [demiryolumusteri] NVARCHAR(255) NULL,
    [denizmusteri] NVARCHAR(255) NULL,
    [depomusteri] NVARCHAR(255) NULL,
    [email] NVARCHAR(255) NULL,
    [finansfaturavadegun] NVARCHAR(255) NULL,
    [finodemesekil] NVARCHAR(255) NULL,
    [firmadurumid] NVARCHAR(64) NULL,
    [firmaid] NVARCHAR(64) NULL,
    [havamusteri] NVARCHAR(255) NULL,
    [id] NVARCHAR(64) NULL,
    [ihrmusteri] NVARCHAR(255) NULL,
    [ilceid] NVARCHAR(64) NULL,
    [ithmusteri] NVARCHAR(255) NULL,
    [karamusteri] NVARCHAR(255) NULL,
    [muhasebekod] NVARCHAR(255) NULL,
    [name] NVARCHAR(255) NULL,
    [phone] NVARCHAR(255) NULL,
    [sahistuzel] NVARCHAR(255) NULL,
    [satici] NVARCHAR(255) NULL,
    [sehirid] NVARCHAR(64) NULL,
    [sirketid] NVARCHAR(64) NULL,
    [tax_number] NVARCHAR(255) NULL,
    [tax_office] NVARCHAR(255) NULL,
    [telefon1] NVARCHAR(255) NULL,
    [trmusteri] NVARCHAR(255) NULL,
    [ulkeid] NVARCHAR(64) NULL,
    [user_permission_page_id] NVARCHAR(64) NULL,
    [vergidaire] NVARCHAR(255) NULL,
    [vergidaireid] NVARCHAR(64) NULL,
    [vergino] NVARCHAR(255) NULL,
    [yicimusteri] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_firmatemsilci', 'U') IS NULL
CREATE TABLE dbo.[sbr_firmatemsilci] (
    [ad] NVARCHAR(255) NULL,
    [address] NVARCHAR(255) NULL,
    [avatar] NVARCHAR(255) NULL,
    [city_id] NVARCHAR(64) NULL,
    [contact_language] NVARCHAR(255) NULL,
    [contact_person] NVARCHAR(255) NULL,
    [country_id] NVARCHAR(64) NULL,
    [discount] NVARCHAR(255) NULL,
    [district_id] NVARCHAR(64) NULL,
    [email] NVARCHAR(255) NULL,
    [firmaid] NVARCHAR(64) NULL,
    [firmatemsilciid] NVARCHAR(64) NULL,
    [id] NVARCHAR(64) NULL,
    [individual_personal] NVARCHAR(255) NULL,
    [instime] DATETIME NULL,
    [insuser] NVARCHAR(255) NULL,
    [invoice_type] NVARCHAR(255) NULL,
    [musteritemsilcisi] NVARCHAR(255) NULL,
    [name] NVARCHAR(255) NULL,
    [phone] NVARCHAR(255) NULL,
    [phone_country_id] NVARCHAR(64) NULL,
    [satistemsilcisi] NVARCHAR(255) NULL,
    [siber_id] NVARCHAR(64) NULL,
    [tax_number] NVARCHAR(255) NULL,
    [tax_office] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_ilce', 'U') IS NULL
CREATE TABLE dbo.[sbr_ilce] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_kzgelirgider', 'U') IS NULL
CREATE TABLE dbo.[sbr_kzgelirgider] (
    [kzgelirgiderid] INT NULL
);
GO

IF OBJECT_ID('dbo.sbr_malcinsi', 'U') IS NULL
CREATE TABLE dbo.[sbr_malcinsi] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_odemesekli', 'U') IS NULL
CREATE TABLE dbo.[sbr_odemesekli] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_sehir', 'U') IS NULL
CREATE TABLE dbo.[sbr_sehir] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_teslimsekli', 'U') IS NULL
CREATE TABLE dbo.[sbr_teslimsekli] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_ulke', 'U') IS NULL
CREATE TABLE dbo.[sbr_ulke] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sbr_vergidaire', 'U') IS NULL
CREATE TABLE dbo.[sbr_vergidaire] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sdn_rezervasyondurum', 'U') IS NULL
CREATE TABLE dbo.[sdn_rezervasyondurum] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sfy_edurum', 'U') IS NULL
CREATE TABLE dbo.[sfy_edurum] (
    [edurumid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.sfy_efatura', 'U') IS NULL
CREATE TABLE dbo.[sfy_efatura] (
    [doviztip] NVARCHAR(255) NULL,
    [efatura] NVARCHAR(255) NULL,
    [efaturaid] NVARCHAR(64) NULL,
    [efaturano] NVARCHAR(255) NULL,
    [entegrator] NVARCHAR(255) NULL,
    [faturadurumkod] NVARCHAR(255) NULL,
    [faturano] NVARCHAR(255) NULL,
    [faturaseri] NVARCHAR(255) NULL,
    [faturaseriyil] NVARCHAR(255) NULL,
    [faturatarih] DATETIME NULL,
    [gc] NVARCHAR(255) NULL,
    [gcad] NVARCHAR(255) NULL,
    [instime] DATETIME NULL,
    [insuser] NVARCHAR(255) NULL,
    [kdv_tutar] DECIMAL(18,4) NULL,
    [musteriid] NVARCHAR(64) NULL,
    [odeme_kuru] NVARCHAR(255) NULL,
    [odenecek_toplamtutar] DECIMAL(18,4) NULL,
    [portalefaturaid] NVARCHAR(64) NULL,
    [senaryo] NVARCHAR(255) NULL,
    [siberefaturano] NVARCHAR(255) NULL,
    [sirketid] NVARCHAR(64) NULL,
    [statukod] NVARCHAR(255) NULL,
    [tamamlandi] NVARCHAR(255) NULL,
    [tarih] DATETIME NULL,
    [tedarikciid] NVARCHAR(64) NULL,
    [tip] NVARCHAR(255) NULL,
    [toplamtutar] DECIMAL(18,4) NULL,
    [vergi_kuru] NVARCHAR(255) NULL,
    [vergi_tutar] DECIMAL(18,4) NULL,
    [vergilendirilecek_toplamtutar] DECIMAL(18,4) NULL,
    [zarfid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.sfy_efaturadetay', 'U') IS NULL
CREATE TABLE dbo.[sfy_efaturadetay] (
    [efaturadetayid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.sfy_efirma', 'U') IS NULL
CREATE TABLE dbo.[sfy_efirma] (
    [efirmaid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.sfy_modulkalem', 'U') IS NULL
CREATE TABLE dbo.[sfy_modulkalem] (
    [aciklama] NVARCHAR(255) NULL,
    [birimfiyat] DECIMAL(18,4) NULL,
    [dovizkod] NVARCHAR(255) NULL,
    [firmaid] NVARCHAR(64) NULL,
    [gc] NVARCHAR(255) NULL,
    [id] NVARCHAR(64) NULL,
    [item_id] NVARCHAR(64) NULL,
    [kalemid] NVARCHAR(64) NULL,
    [kayitgiren] NVARCHAR(255) NULL,
    [kayitgiristarih] DATETIME NULL,
    [kdvoran] DECIMAL(18,4) NULL,
    [kdvtutar] DECIMAL(18,4) NULL,
    [load_number] NVARCHAR(255) NULL,
    [load_number_work_type] NVARCHAR(255) NULL,
    [miktar] DECIMAL(18,4) NULL,
    [modulid] NVARCHAR(64) NULL,
    [modulkalemid] NVARCHAR(64) NULL,
    [modulkod] NVARCHAR(255) NULL,
    [rezervasyondanaktarildi] NVARCHAR(255) NULL,
    [subeid] NVARCHAR(64) NULL,
    [toplamtutar] DECIMAL(18,4) NULL,
    [tutar] DECIMAL(18,4) NULL,
    [work_type] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sfy_modulkayit', 'U') IS NULL
CREATE TABLE dbo.[sfy_modulkayit] (
    [ad] NVARCHAR(255) NULL,
    [description] NVARCHAR(255) NULL,
    [item_id] NVARCHAR(64) NULL,
    [modulkalemid] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.skn_arac', 'U') IS NULL
CREATE TABLE dbo.[skn_arac] (
    [aracdurum] NVARCHAR(255) NULL,
    [aracid] NVARCHAR(64) NULL,
    [aracsahip] NVARCHAR(255) NULL,
    [aractip] NVARCHAR(255) NULL,
    [aractur] INT NULL,
    [baglifirmaid] NVARCHAR(64) NULL,
    [boy] NVARCHAR(255) NULL,
    [code] NVARCHAR(255) NULL,
    [en] NVARCHAR(255) NULL,
    [grupsirketid] NVARCHAR(64) NULL,
    [id] NVARCHAR(64) NULL,
    [kapasite] DECIMAL(18,4) NULL,
    [kayitgiren] NVARCHAR(255) NULL,
    [kayitgiristarih] DATETIME NULL,
    [km] DECIMAL(18,4) NULL,
    [plakano] NVARCHAR(255) NULL,
    [romorkcins] NVARCHAR(255) NULL,
    [siber_id] NVARCHAR(64) NULL,
    [sirketid] NVARCHAR(64) NULL,
    [uluslararasi] NVARCHAR(255) NULL,
    [yici] NVARCHAR(255) NULL,
    [yukseklik] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_kalem', 'U') IS NULL
CREATE TABLE dbo.[skn_kalem] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_kapcins', 'U') IS NULL
CREATE TABLE dbo.[skn_kapcins] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_pozisyon', 'U') IS NULL
CREATE TABLE dbo.[skn_pozisyon] (
    [araccikistarih] DATETIME NULL,
    [baslangicsehirid] NVARCHAR(64) NULL,
    [bitissehirid] NVARCHAR(64) NULL,
    [cektirmefirmaid] NVARCHAR(64) NULL,
    [cikistarih] DATETIME NULL,
    [departmanid] NVARCHAR(64) NULL,
    [donustarih] DATETIME NULL,
    [durumid] NVARCHAR(64) NULL,
    [hafta] INT NULL,
    [haftayil] INT NULL,
    [id] NVARCHAR(64) NULL,
    [isturu] NVARCHAR(255) NULL,
    [kayitgiren] NVARCHAR(255) NULL,
    [kayitgiristarih] DATETIME NULL,
    [name] NVARCHAR(255) NULL,
    [planlananbitistarih] DATETIME NULL,
    [pozisyonid] NVARCHAR(64) NULL,
    [romorkaracsahip] NVARCHAR(255) NULL,
    [romorkcins] NVARCHAR(255) NULL,
    [romorkcinsad] NVARCHAR(255) NULL,
    [romorkid] NVARCHAR(64) NULL,
    [seferid] NVARCHAR(64) NULL,
    [seferno] INT NULL,
    [seferturid] NVARCHAR(64) NULL,
    [siber_id] NVARCHAR(64) NULL,
    [sirano] INT NULL,
    [sirketid] NVARCHAR(64) NULL,
    [subeid] NVARCHAR(64) NULL,
    [tahminivaristarihi] DATETIME NULL,
    [yuklemesehirid] NVARCHAR(64) NULL,
    [yuklemetarih] DATETIME NULL
);
GO

IF OBJECT_ID('dbo.skn_pozisyondurum', 'U') IS NULL
CREATE TABLE dbo.[skn_pozisyondurum] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_rezervasyon', 'U') IS NULL
CREATE TABLE dbo.[skn_rezervasyon] (
    [aciklama] NVARCHAR(255) NULL,
    [aliciid] NVARCHAR(64) NULL,
    [bosaltmaulkeid] NVARCHAR(64) NULL,
    [calismasekli] NVARCHAR(255) NULL,
    [departmanid] NVARCHAR(64) NULL,
    [durumid] NVARCHAR(64) NULL,
    [gecerliliktarih] DATETIME NULL,
    [gondericiid] NVARCHAR(64) NULL,
    [instime] DATETIME NULL,
    [insuser] NVARCHAR(255) NULL,
    [istenenromorkcins] NVARCHAR(255) NULL,
    [isturu] NVARCHAR(255) NULL,
    [musteriid] NVARCHAR(64) NULL,
    [musteritemsilcisi] NVARCHAR(255) NULL,
    [navlunfirmaid] NVARCHAR(64) NULL,
    [odemesekliid] NVARCHAR(64) NULL,
    [ontasimatarafimizdanyapilir] NVARCHAR(255) NULL,
    [operasyonyetkilisikod2] NVARCHAR(255) NULL,
    [pazarlamabildirimtarih] DATETIME NULL,
    [rezervasyonid] NVARCHAR(64) NULL,
    [rezervasyonno] NVARCHAR(255) NULL,
    [rezervasyonnoint] INT NULL,
    [satistemsilcisi2kod] NVARCHAR(255) NULL,
    [satistemsilcisikod] NVARCHAR(255) NULL,
    [sirketid] NVARCHAR(64) NULL,
    [sontasimatarafimizdanyapilir] NVARCHAR(255) NULL,
    [subeid] NVARCHAR(64) NULL,
    [talimatgelissekli] NVARCHAR(255) NULL,
    [talimatgelistarih] DATETIME NULL,
    [yil] INT NULL,
    [yukid] NVARCHAR(64) NULL,
    [yuklemetip] NVARCHAR(255) NULL,
    [yuklemeulkeid] NVARCHAR(64) NULL,
    [yukturkod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_rezervasyontarife', 'U') IS NULL
CREATE TABLE dbo.[skn_rezervasyontarife] (
    [alisbirimtutar] DECIMAL(18,4) NULL,
    [alisdovizkod] NVARCHAR(255) NULL,
    [alisfirmaid] NVARCHAR(64) NULL,
    [aliskdvoran] DECIMAL(18,4) NULL,
    [aliskdvtutar] DECIMAL(18,4) NULL,
    [alistoplamtutar] DECIMAL(18,4) NULL,
    [id] NVARCHAR(64) NULL,
    [kalemid] NVARCHAR(64) NULL,
    [kdvoran] DECIMAL(18,4) NULL,
    [miktar] DECIMAL(18,4) NULL,
    [name] NVARCHAR(255) NULL,
    [offer_date] DATETIME NULL,
    [rezervasyonid] NVARCHAR(64) NULL,
    [rezervasyontarifeid] NVARCHAR(64) NULL,
    [satisbirimtutar] DECIMAL(18,4) NULL,
    [satisdovizkod] NVARCHAR(255) NULL,
    [satisfirmaid] NVARCHAR(64) NULL,
    [satiskdvtutar] DECIMAL(18,4) NULL,
    [satistoplamtutar] DECIMAL(18,4) NULL,
    [siber_id] NVARCHAR(64) NULL,
    [status_type_id] NVARCHAR(64) NULL,
    [tarih] DATETIME NULL,
    [tasimasekli] NVARCHAR(255) NULL,
    [updated_at] DATETIME NULL,
    [user_id] NVARCHAR(64) NULL,
    [user_permission_page_id] NVARCHAR(64) NULL
);
GO

IF OBJECT_ID('dbo.skn_rezervasyonyukkoli', 'U') IS NULL
CREATE TABLE dbo.[skn_rezervasyonyukkoli] (
    [boy] NVARCHAR(255) NULL,
    [burutagirlik] DECIMAL(18,4) NULL,
    [en] NVARCHAR(255) NULL,
    [hacim] DECIMAL(18,4) NULL,
    [istiflenemez] NVARCHAR(255) NULL,
    [kapadet] INT NULL,
    [kapid] NVARCHAR(64) NULL,
    [lademetre] DECIMAL(18,4) NULL,
    [malcinsid] NVARCHAR(64) NULL,
    [netagirlik] DECIMAL(18,4) NULL,
    [rezervasyonid] NVARCHAR(64) NULL,
    [rezyukkoliid] NVARCHAR(64) NULL,
    [siber_id] NVARCHAR(64) NULL,
    [turkcetanim] NVARCHAR(255) NULL,
    [yukseklik] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_sabittanim', 'U') IS NULL
CREATE TABLE dbo.[skn_sabittanim] (
    [grupkod] INT NULL
);
GO

IF OBJECT_ID('dbo.skn_sefer', 'U') IS NULL
CREATE TABLE dbo.[skn_sefer] (
    [aracsahip] NVARCHAR(255) NULL,
    [aracsahipad] NVARCHAR(255) NULL,
    [cikistarih] DATETIME NULL,
    [donustarih] DATETIME NULL,
    [seferid] NVARCHAR(64) NULL,
    [seferno] INT NULL,
    [sirketid] NVARCHAR(64) NULL,
    [subeid] NVARCHAR(64) NULL,
    [yici] NVARCHAR(255) NULL,
    [yil] INT NULL
);
GO

IF OBJECT_ID('dbo.skn_yuk', 'U') IS NULL
CREATE TABLE dbo.[skn_yuk] (
    [_bosaltmakita] NVARCHAR(255) NULL,
    [_bosaltmaulke] NVARCHAR(255) NULL,
    [_yuklemekita] NVARCHAR(255) NULL,
    [_yuklemeulke] NVARCHAR(255) NULL,
    [aliciid] NVARCHAR(64) NULL,
    [aracyuksekligi] NVARCHAR(255) NULL,
    [bagliyukno] NVARCHAR(255) NULL,
    [bagliyuknoisturu] NVARCHAR(255) NULL,
    [bildirimyapankullanicikod] NVARCHAR(255) NULL,
    [calismasekli] NVARCHAR(255) NULL,
    [cmrduzenlenecek] NVARCHAR(255) NULL,
    [departmanid] NVARCHAR(64) NULL,
    [dovizkod] NVARCHAR(255) NULL,
    [durumid] NVARCHAR(64) NULL,
    [fcrduzenlenecek] NVARCHAR(255) NULL,
    [firmaid] NVARCHAR(64) NULL,
    [gondericiid] NVARCHAR(64) NULL,
    [hacimcarpan] DECIMAL(18,4) NULL,
    [hazirolmatarih] DATETIME NULL,
    [istenenromorkcins] NVARCHAR(255) NULL,
    [istenenvaristarihi] DATETIME NULL,
    [isturu] NVARCHAR(255) NULL,
    [kamyonda] NVARCHAR(255) NULL,
    [kayitgiren] NVARCHAR(255) NULL,
    [kayitgiristarih] DATETIME NULL,
    [kuyrukta] NVARCHAR(255) NULL,
    [lademetrecarpan] DECIMAL(18,4) NULL,
    [musteridenalinistarih] DATETIME NULL,
    [musteritemsilcisi2ad] NVARCHAR(255) NULL,
    [musteritemsilcisiad] NVARCHAR(255) NULL,
    [odemesekliid] NVARCHAR(64) NULL,
    [ontasimatarafimizdanyapilir] NVARCHAR(255) NULL,
    [operasyondepartmanid] NVARCHAR(64) NULL,
    [pozisyonid] NVARCHAR(64) NULL,
    [rezervasyonid] NVARCHAR(64) NULL,
    [satistemsilcisikod] NVARCHAR(255) NULL,
    [sirketid] NVARCHAR(64) NULL,
    [sontasimatarafimizdanyapilir] NVARCHAR(255) NULL,
    [subeid] NVARCHAR(64) NULL,
    [talimatgelissekli] NVARCHAR(255) NULL,
    [talimatgelistarihi] DATETIME NULL,
    [teslimsekil] NVARCHAR(255) NULL,
    [toplamagirlik] DECIMAL(18,4) NULL,
    [toplamhacim] DECIMAL(18,4) NULL,
    [toplamkap] NVARCHAR(255) NULL,
    [toplamlademetre] DECIMAL(18,4) NULL,
    [toplamlademetrem3] DECIMAL(18,4) NULL,
    [ucretagirlik] DECIMAL(18,4) NULL,
    [updtime] DATETIME NULL,
    [upduser] NVARCHAR(255) NULL,
    [yil] INT NULL,
    [yukid] NVARCHAR(64) NULL,
    [yuklemetip] NVARCHAR(255) NULL,
    [yukno] NVARCHAR(255) NULL,
    [yuknoisturu] NVARCHAR(255) NULL,
    [yukturkod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_yukaktarma', 'U') IS NULL
CREATE TABLE dbo.[skn_yukaktarma] (
    [pozisyonid] NVARCHAR(64) NULL,
    [romorkid] NVARCHAR(64) NULL,
    [tarih] DATETIME NULL,
    [yerid] NVARCHAR(64) NULL,
    [yukaktarmaid] NVARCHAR(64) NULL,
    [yukid] NVARCHAR(64) NULL,
    [yuklemebosaltma] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_yukdurum', 'U') IS NULL
CREATE TABLE dbo.[skn_yukdurum] (
    [id] NVARCHAR(64) NULL,
    [ad] NVARCHAR(255) NULL,
    [kod] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.skn_yukkoli', 'U') IS NULL
CREATE TABLE dbo.[skn_yukkoli] (
    [boy] NVARCHAR(255) NULL,
    [burutagirlik] DECIMAL(18,4) NULL,
    [en] NVARCHAR(255) NULL,
    [hacim] DECIMAL(18,4) NULL,
    [istiflenemez] NVARCHAR(255) NULL,
    [kapadet] INT NULL,
    [kapid] NVARCHAR(64) NULL,
    [lademetre] DECIMAL(18,4) NULL,
    [malcinsid] NVARCHAR(64) NULL,
    [netagirlik] DECIMAL(18,4) NULL,
    [turkcetanim] NVARCHAR(255) NULL,
    [yukid] NVARCHAR(64) NULL,
    [yukkoliid] NVARCHAR(64) NULL,
    [yukseklik] NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.sky_kullanici', 'U') IS NULL
CREATE TABLE dbo.[sky_kullanici] (
    [engelle] NVARCHAR(255) NULL
);
GO
