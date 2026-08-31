-- Teklifi Siber'e aktarma (transfer_to_siber) icin gereken kolonlar.
USE Siber2022;
GO
DROP TABLE IF EXISTS skn_rezervasyon, skn_rezervasyonyukkoli, skn_rezervasyontarife;
GO
-- yukid: teklif -> yuk baglantisi (Siber Entegrasyon Raporu 6.2 adim 8). Gercek
-- Siber'de uniqueidentifier; burada digerleriyle ayni sekilde NVARCHAR tutuluyor.
CREATE TABLE skn_rezervasyon (
    rezervasyonid NVARCHAR(64), sirketid NVARCHAR(64), subeid NVARCHAR(64),
    talimatgelissekli NVARCHAR(64), rezervasyonno INT, rezervasyonnoint NVARCHAR(16),
    istenenromorkcins NVARCHAR(64), isturu NVARCHAR(64), yuklemetip NVARCHAR(64),
    yukturkod NVARCHAR(64), pazarlamabildirimtarih DATETIME, talimatgelistarih DATETIME,
    gecerliliktarih DATETIME, odemesekliid NVARCHAR(64),
    ontasimatarafimizdanyapilir INT, sontasimatarafimizdanyapilir INT,
    musteriid NVARCHAR(64), navlunfirmaid NVARCHAR(64), gondericiid NVARCHAR(64),
    aliciid NVARCHAR(64), durumid NVARCHAR(64), musteritemsilcisi NVARCHAR(255),
    satistemsilcisikod NVARCHAR(64), departmanid NVARCHAR(64), aciklama NVARCHAR(1000),
    yil INT, instime DATETIME, insuser NVARCHAR(64),
    yuklemeulkeid NVARCHAR(64), bosaltmaulkeid NVARCHAR(64), calismasekli INT,
    yukid NVARCHAR(64) NULL);

CREATE TABLE skn_rezervasyonyukkoli (
    rezyukkoliid NVARCHAR(64), rezervasyonid NVARCHAR(64), kapadet INT,
    en DECIMAL(18,4), boy DECIMAL(18,4), yukseklik DECIMAL(18,4),
    malcinsid NVARCHAR(64), kapid NVARCHAR(64), turkcetanim NVARCHAR(255),
    hacim DECIMAL(18,4), burutagirlik DECIMAL(18,4), netagirlik DECIMAL(18,4),
    lademetre DECIMAL(18,4), istiflenemez INT);

-- Tarife tek tabloda IKI ayri sutun grubu tutar: kalem alissa alis*, satissa satis*
-- doldurulur (Siber Entegrasyon Raporu 5.1 adim 6). Sutun adlari 192.168.1.101
-- uzerindeki gercek Siber2022 semasindan dogrulandi.
CREATE TABLE skn_rezervasyontarife (
    rezervasyontarifeid NVARCHAR(64), rezervasyonid NVARCHAR(64), tarih DATETIME,
    miktar DECIMAL(18,4),
    alisdovizkod NVARCHAR(16), alisbirimtutar DECIMAL(18,4),
    alistoplamtutar DECIMAL(18,4), alisfirmaid NVARCHAR(64),
    satisdovizkod NVARCHAR(16), satisbirimtutar DECIMAL(18,4),
    satistoplamtutar DECIMAL(18,4), satisfirmaid NVARCHAR(64),
    kalemid NVARCHAR(64), tasimasekli NVARCHAR(64),
    kdvoran DECIMAL(18,4), aliskdvoran DECIMAL(18,4));
GO
PRINT 'rezervasyon tablolari hazir';
GO
