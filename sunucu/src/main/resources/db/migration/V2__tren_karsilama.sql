-- Tren siparis karsilama kayitlari (B.15).
--
-- DIKKAT: sefer/fiyat/kart SATIRI YOKTUR (mimari kural 3 — tren tarafi
-- deterministik ve durumsuzdur). Buraya yalnizca "bu vadinin su seferinde
-- su karti kim karsiladi" yazilir; C.5 cakisma kurali (ilk ulasan kazanir)
-- birincil anahtarla zorlanir.
CREATE TABLE tren_karsilama (
    vadi_id    UUID    NOT NULL,
    sefer_no   BIGINT  NOT NULL,
    kart       INT     NOT NULL,
    oyuncu_id  UUID    NOT NULL,
    karsilandi TIMESTAMP NOT NULL,
    PRIMARY KEY (vadi_id, sefer_no, kart)
);
