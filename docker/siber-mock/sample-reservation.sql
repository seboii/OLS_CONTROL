-- Teklifin Siber'deki rezervasyon karşılığı (dönüşüm doğrulaması için).
USE Siber2022;
GO
DELETE FROM skn_rezervasyon;
INSERT INTO skn_rezervasyon
  (rezervasyonid, istenenromorkcins, isturu, musteriid, gondericiid, aliciid, odemesekliid, durumid, departmanid)
VALUES
  ('rez-0000-0001', '1', '1',
   'aaaaaaa1-0000-0000-0000-000000000001',
   'aaaaaaa1-0000-0000-0000-000000000001',
   'aaaaaaa1-0000-0000-0000-000000000002',
   '77777777-0000-0000-0000-000000000001',
   '66666666-0000-0000-0000-000000000002',
   '88888888-0000-0000-0000-000000000001');

DELETE FROM sfy_modulkayit;
INSERT INTO sfy_modulkayit (ad, modulid, modulkod) VALUES ('DUMMY', 'mod-1', 'YUK');
GO
PRINT 'rezervasyon hazir';
GO
