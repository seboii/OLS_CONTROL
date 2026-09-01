-- Muhasebe/finans tabloları — Siber'in sfy_* şemasının akış doğrulaması için
-- yeterli alt kümesi.
--
-- Yalnızca uygulamanın OKUDUĞU ve YAZDIĞI sütunlar var; gerçek Siber'de
-- sfy_gelirgider 194 sütun taşıyor ve tamamını yansıtmak taklidi okunmaz
-- yapardı.
--
-- İki davranış BİLEREK birebir korundu, çünkü kod bunlara dayanıyor:
--   * kimlikler uniqueidentifier — okuma sorguları CAST(... AS VARCHAR(64))
--     kullanmak zorunda, taklit NVARCHAR olsaydı bu kural test edilmezdi;
--   * faturaintid ve kayitgiris_sirano IDENTITY — yazıcı bu sütunları
--     INSERT'e koymaz, numarayı SQL Server üretir.
USE Siber2022;
GO

IF OBJECT_ID('dbo.sfy_hesapplan', 'U') IS NULL
CREATE TABLE dbo.[sfy_hesapplan] (
    [hesapplanid] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [sirketid]    UNIQUEIDENTIFIER NULL,
    [hesapkod]    VARCHAR(20)  NULL,
    [ad]          NVARCHAR(200) NULL,
    [ad2]         NVARCHAR(200) NULL,
    [seviye]      TINYINT NULL,
    [pasif]       BIT NULL
);
GO

IF OBJECT_ID('dbo.sfy_gelirgider', 'U') IS NULL
CREATE TABLE dbo.[sfy_gelirgider] (
    [gelirgiderid]    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [faturaintid]     INT IDENTITY(1,1) NOT NULL,
    [sirketid]        UNIQUEIDENTIFIER NOT NULL,
    [subeid]          UNIQUEIDENTIFIER NOT NULL,
    [gc]              CHAR(1) NULL,
    [faturaserino]    NVARCHAR(20) NULL,
    [faturano]        NVARCHAR(40) NULL,
    [faturatarihi]    SMALLDATETIME NOT NULL,
    [vadetarihi]      SMALLDATETIME NULL,
    [firmaid]         UNIQUEIDENTIFIER NOT NULL,
    [firmaad]         NVARCHAR(200) NULL,
    [dovizkod]        CHAR(3) NULL,
    [dovizkur]        FLOAT NULL,
    [tutar]           FLOAT NULL,
    [kdvtutar]        FLOAT NULL,
    [toplamtutar]     FLOAT NULL,
    [tutartl]         FLOAT NULL,
    [kdvtutartl]      FLOAT NULL,
    [toplamtutartl]   FLOAT NULL,
    [aciklama]        NVARCHAR(510) NULL,
    [belgeno]         NVARCHAR(100) NULL,
    [modulid]         UNIQUEIDENTIFIER NULL,
    [modulkod]        NVARCHAR(12) NULL,
    [onay]            BIT NULL,
    [onaytarih]       DATETIME NULL,
    [kayitgiristarih] SMALLDATETIME NULL,
    [kayitgiren]      VARCHAR(100) NULL,
    [instime]         DATETIME NULL,
    [updtime]         DATETIME NULL
);
GO

IF OBJECT_ID('dbo.sfy_gelirgiderdetay', 'U') IS NULL
CREATE TABLE dbo.[sfy_gelirgiderdetay] (
    [gelirgiderdetayid]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [kayitgiris_sirano]  BIGINT IDENTITY(1,1) NOT NULL,
    [gelirgiderid]       UNIQUEIDENTIFIER NOT NULL,
    [gelirgider]         BIT NULL,
    [firmaid]            UNIQUEIDENTIFIER NULL,
    [kalemid]            UNIQUEIDENTIFIER NULL,
    [kalemyabanciad]     NVARCHAR(200) NULL,
    [dovizkod]           CHAR(3) NULL,
    [dovizkur]           FLOAT NULL,
    [kdvoran]            FLOAT NULL,
    [miktar]             FLOAT NULL,
    [birimfiyat]         FLOAT NULL,
    [tutar]              FLOAT NULL,
    [kdvtutar]           FLOAT NULL,
    [tutartl]            FLOAT NULL,
    [kdvtutartl]         FLOAT NULL,
    [aciklama]           NVARCHAR(510) NULL,
    [belgeno]            NVARCHAR(100) NULL,
    [belgetarih]         SMALLDATETIME NULL,
    [modulid]            UNIQUEIDENTIFIER NULL,
    [modulkod]           NVARCHAR(12) NULL,
    -- Gerçek Siber'de var ama HİÇ doldurulmamış (133.908 satırın tamamı boş);
    -- kod buna güvenmiyor, yük bağı başlıktaki modulid'den kuruluyor.
    [yukid]              UNIQUEIDENTIFIER NULL
);
GO

IF OBJECT_ID('dbo.sfy_tahsilatodeme', 'U') IS NULL
CREATE TABLE dbo.[sfy_tahsilatodeme] (
    [tahsilatodemeid] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [sirketid]        UNIQUEIDENTIFIER NULL,
    [makbuzno]        NVARCHAR(40) NULL,
    [makbuztarih]     SMALLDATETIME NULL,
    [vadetarih]       SMALLDATETIME NULL,
    [islemtur]        INT NULL,
    [borcid]          UNIQUEIDENTIFIER NULL,
    [borcad]          NVARCHAR(200) NULL,
    [borchesapkod]    VARCHAR(20) NULL,
    [alacakid]        UNIQUEIDENTIFIER NULL,
    [alacakad]        NVARCHAR(200) NULL,
    [alacakhesapkod]  VARCHAR(20) NULL,
    [dovizkod]        CHAR(3) NULL,
    [dovizkur]        FLOAT NULL,
    [tutar]           FLOAT NULL,
    [tutartl]         FLOAT NULL,
    [aciklama]        NVARCHAR(510) NULL,
    [modulid]         UNIQUEIDENTIFIER NULL,
    [modulkod]        NVARCHAR(12) NULL,
    [kayitgiristarih] SMALLDATETIME NULL,
    [kayitgiren]      VARCHAR(100) NULL
);
GO

IF OBJECT_ID('dbo.sfy_fis', 'U') IS NULL
CREATE TABLE dbo.[sfy_fis] (
    [fisid]               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [sirketid]            UNIQUEIDENTIFIER NULL,
    [fistur]              TINYINT NULL,
    [fistarih]            SMALLDATETIME NULL,
    [fisno]               INT NULL,
    [yevmiyeno]           INT NULL,
    [aciklama]            NVARCHAR(510) NULL,
    [doviztur]            CHAR(3) NULL,
    [muhasebebelgeno]     VARCHAR(20) NULL,
    [muhasebebelgetarih]  DATETIME NULL,
    [kontroledildi]       BIT NULL,
    [kayitgiristarih]     SMALLDATETIME NULL,
    [insuser]             VARCHAR(128) NULL
);
GO

IF OBJECT_ID('dbo.sfy_fisdetay', 'U') IS NULL
CREATE TABLE dbo.[sfy_fisdetay] (
    [fisdetayid]  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [fisid]       UNIQUEIDENTIFIER NOT NULL,
    [sirketid]    UNIQUEIDENTIFIER NULL,
    [hesapkod]    VARCHAR(20) NULL,
    [borc]        FLOAT NULL,
    [alacak]      FLOAT NULL,
    [borcdoviz]   FLOAT NULL,
    [alacakdoviz] FLOAT NULL,
    [doviztur]    CHAR(3) NULL,
    [dovizkur]    FLOAT NULL,
    [aciklama]    NVARCHAR(510) NULL,
    -- Cari bağı BURADAN kurulur. sbr_firma.muhasebekod gerçek Siber'de
    -- 7.429 firmanın hiçbirinde dolu değil; kartoteksid tek güvenilir yol.
    [kartoteksid] UNIQUEIDENTIFIER NULL,
    -- Kaynak belge: fatura ya da tahsilat/ödeme kimliği.
    [entegreid]   UNIQUEIDENTIFIER NULL,
    [belgeno]     VARCHAR(20) NULL,
    [belgetarih]  SMALLDATETIME NULL,
    [vadetarih]   SMALLDATETIME NULL,
    [sirano]      BIGINT NULL,
    [instime]     DATETIME NULL,
    [updtime]     DATETIME NULL
);
GO

-- Örnek hesap planı: mizan ve fiş ekranlarının boş kalmaması için asgari küme.
IF NOT EXISTS (SELECT 1 FROM dbo.sfy_hesapplan)
INSERT INTO dbo.sfy_hesapplan (sirketid, hesapkod, ad, seviye, pasif) VALUES
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '100',             'KASA',                1, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '102',             'BANKALAR',            1, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '120',             'ALICILAR',            1, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '120 02 01 0001',  'YURT DIŞI ALICILAR',  4, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '320',             'SATICILAR',           1, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '320 01 01 0001',  'YURT İÇİ SATICILAR',  4, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '601',             'YURT DIŞI SATIŞLAR',  1, 0),
    ('BA4888B1-A2B0-4142-B273-92481D932EAD', '740',             'HİZMET ÜRETİM MALİYETİ', 1, 0);
GO
