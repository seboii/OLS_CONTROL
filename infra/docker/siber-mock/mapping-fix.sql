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

-- skn_yukaktarma BURADA YENİDEN OLUŞTURULMUYOR: sample-movements.sql zaten
-- ETL'in okuduğu kolonlarla (birebir aynı 7 kolon) kuruyor. Bu dosya daha önce
-- burayı da DROP+CREATE ediyordu — sample-movements.sql'den SONRA çalışırsa
-- örnek satırı sessizce siliyordu (şema aynı olduğu için hatasız ama veri
-- kaybıyla). Bulunup düzeltildi; sıralama artık docker-compose.yml'de
-- sample-movements.sql'i mapping-fix.sql'den ÖNCE çalıştırıyor.
IF OBJECT_ID('dbo.skn_yukaktarma', 'U') IS NULL
CREATE TABLE skn_yukaktarma (
    yukaktarmaid NVARCHAR(64),
    yuklemebosaltma INT,
    yukid NVARCHAR(64),
    pozisyonid NVARCHAR(64),
    romorkid NVARCHAR(64),
    yerid NVARCHAR(64),
    tarih DATETIME);
GO
