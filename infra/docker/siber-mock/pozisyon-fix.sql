-- skn_pozisyon: yazma tarafının kullandığı ek kolonlar
-- (sample-movements.sql yalnızca ETL'in OKUDUĞU kolonları içeriyordu).
USE Siber2022;
GO
ALTER TABLE skn_pozisyon ADD
    sirketid NVARCHAR(64) NULL,
    subeid NVARCHAR(64) NULL,
    sirano INT NULL,
    kayitgiren NVARCHAR(64) NULL,
    cektirmefirmaid NVARCHAR(64) NULL,
    planlananbitistarih DATETIME NULL,
    haftayil NVARCHAR(8) NULL,
    romorkaracsahip INT NULL,
    id INT IDENTITY(1,1);
GO
UPDATE skn_pozisyon SET haftayil = '2026', romorkaracsahip = 0, sirano = 1;
GO
PRINT 'skn_pozisyon genisletildi';
GO
