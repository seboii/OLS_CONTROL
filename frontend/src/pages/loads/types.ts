/**
 * Yük listesinin paylaşılan tipleri.
 *
 * `LoadsPage.tsx` ile `LoadCard.tsx` arasında ortak; kartın kendi dosyasına
 * koymak sayfayı kendi ana liste tipini bir alt bileşenden almaya zorlardı.
 *
 * Detay ekranının (yalnızca sayfanın kullandığı) tipleri BİLEREK burada değil:
 * onlar tek tüketicili ve sayfayla birlikte değişiyor.
 */
export interface NamedRef {
  id: number;
  name: string | null;
}

export interface LoadTransferItem {
  siber_deleted_at?: string | null;
  id: number;
  load_number: string | null;
  load_number_work_type: string | null;
  created_at: string | null;
  customer_id: NamedRef | null;
  sender_id: NamedRef | null;
  receiver_id: NamedRef | null;
  load_status_id: NamedRef | null;
  usercode_with_notification: NamedRef | null;
  work_type: NamedRef | null;
}
