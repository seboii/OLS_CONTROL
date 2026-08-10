import { defineStore } from "pinia";
import Cookies from "js-cookie";

import axios from "axios";

export const useGeneralStore = defineStore("general", {
  state: () => ({
    user_data: null,
    users: [],
    splash_screen_status: true,
    notification_permission: false,
    mobile_bottom_menu: {
      visible: false,
      menu_in_visible: false,
      child_view_status: [],
    },
    quick_load_status: false,
    new_quick_load_drawer_status: false,
    message_drawer_status: false,
    message_user_list_drawer_status: false,
    message_active_user: null,
    message_management: {
      chat_drawer_status: false,
      active_message_users: [],
      active_user: null,
      active_messages: [],
    },
    notification_management: {
      drawer_status: false,
    },
    mail_management: {
      new_mail_dialog_status: false,
    },
  }),
  getters: {
    chatActiveUser: (state) => state.message_management.active_user,
    chatActiveMessages: (state) => state.message_management.active_messages,
    notificationPermissionStatus: (state) => state.notification_permission,
  },
  actions: {
    SET_SPLASH_SCREEN_STATUS(status) {
      this.splash_screen_status = status;
    },
    SET_MOBILE_BOTTOM_MENU_STATUS(status) {
      this.mobile_bottom_menu.visible = status;
    },
    SET_MOBILE_BOTTOM_MENU_TOGGLE() {
      this.mobile_bottom_menu.visible = !this.mobile_bottom_menu.visible;

      if (this.mobile_bottom_menu.visible) {
        setTimeout(() => {
          this.mobile_bottom_menu.menu_in_visible = this.mobile_bottom_menu.visible;
        }, 200);
      } else {
        this.mobile_bottom_menu.menu_in_visible = this.mobile_bottom_menu.visible;
      }
    },
    SET_QUICK_LOAD_STATUS(status) {
      this.quick_load_status = status;
    },
    SET_NEW_QUICK_LOAD_DRAWER_STATUS(status) {
      this.new_quick_load_drawer_status = status;
    },
    SET_MESSAGE_DRAWER_STATUS(status) {
      this.message_drawer_status = status;
    },
    SET_MESSAGE_USER_LIST_DRAWER_STATUS(status) {
      this.message_user_list_drawer_status = status;
    },
    SET_MESSAGE_ACTIVE_USER(user) {
      this.message_management.active_user = user;
    },
    SET_CHAT_DRAWER_STATUS(status) {
      this.message_management.chat_drawer_status = status;
    },
    OPEN_CHAT_DRAWER(user) {
      this.SET_MESSAGE_ACTIVE_USER(user);
      this.SET_CHAT_DRAWER_STATUS(true);
    },
    CLEAR_ACTIVE_MESSAGE() {
      this.message_management.active_user = null;
      this.message_management.active_messages = [];
    },
    SET_NOTIFICATION_DRAWER_STATUS(status) {
      this.notification_management.drawer_status = status;
    },
    SET_NEW_MAIL_DIALOG_STATUS(status) {
      this.mail_management.new_mail_dialog_status = status;
    },
    SET_NOTIFICATION_PERMISSION_STATUS(status) {
      this.notification_permission = status;
    },
    SET_USER_WRITING({ user, status }) {
      this.users = this.users.map((item) => {
        if (user.id == item.id) {
          item.writing = status;
        }
        return item;
      });
      this.message_management.active_message_users.map((item) => {
        if (user.id == item.id) {
          item.writing = status;
        }
        return item;
      });
    },
    async GET_AUTH() {
      try {
        const res = await axios.get("/api/v1/auth");
        if (res.data.authenticated) {
          this.user_data = res.data.data;
          return true;
        }
        return false;
      } catch (error) {
        return false;
      }
    },
    async LOGOUT() {
      try {
        const res = await axios.post(
          "/api/v1/logout",
          {},
          {
            headers: {
              Authorization: `Bearer ${Cookies.get("token")}`,
            },
          }
        );
        if (res.data.status) {
          Cookies.remove("token");
          window.location.href = "/login";
        }
      } catch (error) {
        console.log(error);
      }
    },
    async GET_ALL_USERS() {
      try {
        const res = await axios.get("/api/v1/message/getUser");
        if (res.data) {
          this.users = res.data.data;
          return res.data.data;
        }
      } catch (error) {
        console.log(error);
      }
    },
    async GET_ACTIVE_MESSAGE_LIST() {
      try {
        const res = await axios.get("/api/v1/message/getUserTotalMessage");
        if (res.data) {
          this.message_management.active_message_users = res.data.data;
          return res.data.data;
        }
      } catch (error) {
        console.log(error);
      }
    },
    async GET_CONVERSION_MESSAGES(receiver_id) {
      try {
        const res = await axios.get("/api/v1/message/getMessage", {
          params: {
            receiver_id: receiver_id ?? this.message_management.active_user.id,
          },
        });
        if (res.data) {
          this.message_management.active_messages = res.data.data;
          return res.data.data;
        }
      } catch (error) {
        console.log(error);
      }
    },
    async SEND_MESSAGE({ receiver_id, content }) {
      try {
        const res = await axios.post("/api/v1/message/save", {
          receiver_id: receiver_id ?? this.message_management.active_user.id,
          content,
          type: "text",
        });
        return res.data;
      } catch (error) {
        console.log(error);
      }
    },
    ADD_NEW_MESSAGE(message) {
      console.log(message);
      if (
        (this.message_management.active_user && message.sender_id == this.message_management.active_user.id && message.receiver_id == this.user_data.id) ||
        (message.sender_id == this.user_data.id && message.receiver_id == this.message_management.active_user.id)
      ) {
        this.message_management.active_messages.push(message);
      }
      let new_user_list = this.message_management.active_message_users.map((user) => {
        if (user.id == message.sender_id || user.id == message.receiver_id) {
          user.last_message = message.content;
          user.last_message_time = message.created_at;
        }
        return user;
      });
      this.message_management.active_message_users = new_user_list;

      // Mesajı gönderen kullanıcıyı dizinin başına taşıma
      const senderIndex = this.message_management.active_message_users.findIndex((user) => user.id == message.sender_id || user.id == message.receiver_id);
      if (senderIndex > -1) {
        const [sender] = this.message_management.active_message_users.splice(senderIndex, 1); // Kullanıcıyı diziden çıkar
        this.message_management.active_message_users.unshift(sender); // Kullanıcıyı dizinin başına ekle
      }
    },
  },
});

export const useFeatureStore = defineStore("feature", {
  state: () => ({
    work_types_modal_status: false,
    departments_modal_status: false,
    payment_types_modal_status: false,
    loading_types_modal_status: false,
    status_types_modal_status: false,
    vessel_types_modal_status: false,
    account_types_modal_status: false,
    transport_types_modal_status: false,
    load_product_types_modal_status: false,
    load_financial_item_types_modal_status: false,
    load_financial_items_modal_status: false,
    movement_types_modal_status: false,
    transit_declaration_document_types_modal_status: false,
    account_group_code_modal_status: false,
    account_plan_modal_status: false,
    destinations_modal_status: false,
  }),
  actions: {
    TOGGLE_MODAL_STATUS(key) {
      this[key] = !this[key];
    },
    SET_WORK_TYPES_MODAL_STATUS(status) {
      this.work_types_modal_status = status;
    },
    SET_DEPARTMENTS_MODAL_STATUS(status) {
      this.departments_modal_status = status;
    },
    SET_PAYMENT_TYPES_MODAL_STATUS(status) {
      this.payment_types_modal_status = status;
    },
    SET_LOADING_TYPES_MODAL_STATUS(status) {
      this.loading_types_modal_status = status;
    },
    SET_STATUS_TYPES_MODAL_STATUS(status) {
      this.status_types_modal_status = status;
    },
    SET_VESSEL_TYPES_MODAL_STATUS(status) {
      this.vessel_types_modal_status = status;
    },
    SET_ACCOUNT_TYPES_MODAL_STATUS(status) {
      this.account_types_modal_status = status;
    },
    SET_TRANSPORT_TYPES_MODAL_STATUS(status) {
      this.transport_types_modal_status = status;
    },
    SET_LOAD_PRODUCT_TYPES_MODAL_STATUS(status) {
      this.load_product_types_modal_status = status;
    },
    SET_LOAD_FINANCIAL_ITEM_TYPES_MODAL_STATUS(status) {
      this.load_financial_item_types_modal_status = status;
    },
    SET_LOAD_FINANCIAL_ITEMS_MODAL_STATUS(status) {
      this.load_financial_items_modal_status = status;
    },
    SET_MOVEMENT_TYPES_MODAL_STATUS(status) {
      this.movement_types_modal_status = status;
    },
    SET_TRANSIT_DECLARATION_DOCUMENT_TYPES_MODAL_STATUS(status) {
      this.transit_declaration_document_types_modal_status = status;
    },
    SET_ACCOUNT_GROUP_CODE_MODAL_STATUS(status) {
      this.account_group_code_modal_status = status;
    },
    SET_ACCOUNT_PLAN_MODAL_STATUS(status) {
      this.account_plan_modal_status = status;
    },
    SET_DESTINATIONS_MODAL_STATUS(status) {
      this.destinations_modal_status = status;
    },
  },
});
