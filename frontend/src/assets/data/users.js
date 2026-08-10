export const users = [
  {
    id: 1,
    name: "Yücel Kulaç",
    title: "Software Developer",
    role_id: "developer",
    email: "yucelkulac@jeetwork.com",
    phone: "0532 123 45 67",
    image: "https://img.freepik.com/premium-photo/happy-man-ai-generated-portrait-user-profile_1119669-1.jpg",
  },
  {
    id: 2,
    name: "Efraim SOYUTÜRK",
    title: "Genel Müdür",
    role_id: "manager",
    email: "efraimsoyuturk@olslog.com",
    phone: "0532 123 45 67",
    image: "https://img.freepik.com/premium-photo/indian-male-model_928503-1124.jpg",
  },
  {
    id: 3,
    name: "Nihan Şahin",
    title: "Depo Yöneticisi",
    role_id: "warehouse_manager",
    email: "nihansahin@jeetwork.com",
    phone: "0532 123 45 67",
    image: "https://www.shutterstock.com/image-photo/cute-girl-25-years-old-260nw-2508419695.jpg",
  },
  {
    id: 4,
    name: "Elif Türedi",
    title: "Satış Uzmanı",
    role_id: "sales_specialist",
    email: "elifturedi@jeetwork.com",
    phone: "0532 123 45 67",
    image: "https://www.shutterstock.com/image-photo/happy-girl-25-years-old-260nw-2508318335.jpg",
  },
  {
    id: 5,
    name: "Hasan ÇALIŞKAN",
    title: "Operasyon Yöneticisi",
    role_id: "manager",
    email: "ahmetyilmaz@olslog.com",
    phone: "0532 123 45 67",
    image: "https://www.for-image.com/wp-content/uploads/2023/01/LinkedIn-studio-headshot-photographer-london-1024x1024.jpg",
  },
  {
    id: 6,
    name: "Ahmet Yılmaz",
    title: "Genel Müdür",
    role_id: "manager",
    email: "ahmetyilmaz@olslog.com",
    phone: "0532 123 45 67",
    image: "https://www.corporatephotographerslondon.com/wp-content/uploads/2023/02/LinkedIn_Profile_Photo.jpg",
  },
];

export const messages = [
  {
    id: 1,
    sender_id: 1,
    receiver_id: 2,
    message: "Merhaba, bu hafta sonu için bir toplantı planlıyoruz. Katılabilir misiniz?",
    date: "2024-10-10T14:00:00",
  },
  {
    id: 2,
    sender_id: 2,
    receiver_id: 1,
    message: "Merhaba, evet katılabilirim. Saat kaçta olacak?",
    date: "2024-10-10T14:02:00",
  },
  {
    id: 3,
    sender_id: 3,
    receiver_id: 4,
    message: "Merhaba, 14:00'da olacak. Katılabilir misiniz?",
    date: "2024-09-01T14:00:00",
  },
  {
    id: 4,
    sender_id: 4,
    receiver_id: 3,
    message: "Merhaba, evet katılabilirim. Teşekkürler.",
    date: "2024-09-01T14:00:00",
  },
  {
    id: 5,
    sender_id: 1,
    receiver_id: 2,
    message: `14:00'de yapacağız.`,
    date: "2024-10-10T14:04:00",
  },
  {
    id: 6,
    sender_id: 2,
    receiver_id: 1,
    message: `Tamam, ben de katılacağım.`,
    date: "2024-10-10T14:05:00",
  },
  {
    id: 7,
    sender_id: 1,
    receiver_id: 4,
    message: `Merhaba, Temmuz ayındaki raporlar hakkında bilgi verebilir misiniz?`,
    date: "2024-10-12T09:43:00",
  },
];

export const GET_USER_LIST = () => {
  return users;
};
export const GET_USER_LIST_EXCEPT = (id) => {
  return users.filter((user) => user.id != id);
};

export const GET_USER = (id) => {
  return users.find((user) => user.id == id);
};

export const GET_SINGLE_USER_MESSAGES = ({ sender, receiver }) => {
  return messages.filter(
    (message) => (message.sender_id === sender && message.receiver_id === receiver) || (message.sender_id === receiver && message.receiver_id === sender)
  );
};

export const SEND_MESSAGE = ({ sender, receiver, message }) => {
  messages.push({
    id: messages.length + 1,
    sender_id: sender,
    receiver_id: receiver,
    message: message,
    date: new Date().toISOString().slice(0, 10),
    time: new Date().toISOString().slice(11, 16),
  });
};

export const GET_USER_MESSAGES = (id) => {
  return messages.filter((message) => message.sender_id === id || message.receiver_id === id);
};

export const MY_CONVERSATIONS = (id) => {
  let user_id_list = [];
  let user_list = [];
  messages.forEach((message) => {
    if (message.sender_id == id || message.receiver_id == id) {
      if (user_id_list.includes(message.sender_id) || user_id_list.includes(message.receiver_id)) {
        return;
      }
      if (message.sender_id == id) {
        user_id_list.push(message.receiver_id);
      } else {
        user_id_list.push(message.sender_id);
      }
    }
  });
  users.forEach((user) => {
    if (user_id_list.includes(user.id)) {
      user_list.push(user);
    }
  });
  return user_list;
};

export const GET_LATEST_USER_MESSAGE = ({ sender, receiver }) => {
  return messages
    .filter(
      (message) => (message.sender_id === sender && message.receiver_id === receiver) || (message.sender_id === receiver && message.receiver_id === sender)
    )
    .pop();
};
