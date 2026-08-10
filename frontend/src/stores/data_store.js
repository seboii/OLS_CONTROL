import { defineStore } from "pinia";
import Cookies from "js-cookie";
import { useGetFirstError } from "@/composables/index.js";
import { toast } from "vue-sonner";
import axios from "axios";
import { startOfMonth, endOfMonth, format } from "date-fns";

export const useDataStore = defineStore("datastore", {
  state: () => ({
    user: {
      data: null,
      role: [],
    },
    country: [],
    currencies: [],
    transit_declaration_document_types: [],
    system_statistics_count: null,
  }),
  actions: {
    async GET_COUNTRIES(params = {}) {
      try {
        const res = await axios.get("/api/v1/country", {
          params: { per_page: 30, ...params },
        });
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_CITIES(params = {}) {
      try {
        const res = await axios.get("/api/v1/city", {
          params: { per_page: 30, ...params },
        });
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DISTRICT(params = {}) {
      try {
        const res = await axios.get("/api/v1/district", {
          params: { per_page: 30, ...params },
        });
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_CUSTOMER({ data }) {
      try {
        const res = await axios.post("/api/v1/account", data, {
          headers: {
            "Content-Type": "multipart/form-data",
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_CUSTOMER({ data }) {
      try {
        const res = await axios.post("/api/v1/account/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_CUSTOMER({ id_list }) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/account",
          data: {
            deletion_id: id_list,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_CUSTOMERS(params = {}) {
      try {
        const res = await axios.get("/api/v1/account", {
          params: { per_page: 30, ...params },
        });
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_ACCOUNT(id) {
      try {
        const res = await axios.get(`/api/v1/account/${id}`);
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD({ data }) {
      try {
        const res = await axios.post("/api/v1/load", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD_AI({ data }) {
      try {
        const res = await axios.post("/api/v1/load/saveAi", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async SEND_LOAD_SIBER(id) {
      try {
        const res = await axios.post("/api/v1/transfer_to_siber", {
          id,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_REAL_LOAD(id) {
      try {
        const res = await axios.post("/api/v1/load_transfer", {
          id,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_LOADS(params = {}) {
      try {
        const res = await axios.get("/api/v1/load", {
          params: { per_page: 30, ...params },
        });
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_LOAD(id) {
      try {
        const res = await axios.get(`/api/v1/load/${id}`);
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD({ data, id }) {
      try {
        const res = await axios.post(`/api/v1/load/${id}`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_OFFER_CONTENT_ITEM(id) {
      try {
        const res = await axios.delete(`/api/v1/load/load_content`, {
          params: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_OFFER_FINANCIAL_ITEM(id) {
      try {
        const res = await axios.delete(`/api/v1/load/load_financial_item`, {
          params: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_WORK_TYPES() {
      try {
        const res = await axios.get("/api/v1/work_type");
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_WORK_TYPE(id) {
      try {
        const res = await axios.get(`/api/v1/work_type/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_WORK_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/work_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_WORK_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/work_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_WORK_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/work_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD_PRODUCT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/product_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD_PRODUCT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/product_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_LOAD_PRODUCT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/product_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD_FINANCIAL_ITEM_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/item_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD_FINANCIAL_ITEM_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/item_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_LOAD_FINANCIAL_ITEM_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/item_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD_FINANCIAL_ITEM({ data }) {
      try {
        const res = await axios.post("/api/v1/financial_item", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD_FINANCIAL_ITEM({ data }) {
      try {
        const res = await axios.put("/api/v1/financial_item", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_LOAD_FINANCIAL_ITEM({ data }) {
      try {
        const res = await axios.post("/api/v1/financial_item/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_MOVEMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/movement_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_MOVEMENT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/movement_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_MOVEMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/movement_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_DEPARTMENT({ data }) {
      try {
        const res = await axios.post("/api/v1/department", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_DEPARTMENT({ data }) {
      try {
        const res = await axios.put("/api/v1/department", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_DEPARTMENT({ data }) {
      try {
        const res = await axios.post("/api/v1/department/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_PAYMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/payment_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_PAYMENT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/payment_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_PAYMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/payment_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOADING_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/loading_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOADING_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/loading_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_LOADING_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/loading_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_STATUS_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/status_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_STATUS_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/status_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_STATUS_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/status_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_VESSEL_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/case_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_VESSEL_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/case_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_VESSEL_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/case_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_ACCOUNT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/account_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_ACCOUNT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/account_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_ACCOUNT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/account_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_TRANSPORT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/transport_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_TRANSPORT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/transport_type", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_TRANSPORT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/transport_type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_PROFILE() {
      try {
        const res = await axios.get("/api/v1/profile");
        this.user.data = res.data.data;
        return res.data.data;
      } catch (error) {
        //toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_PROFILE_GENERAL({ data }) {
      try {
        const res = await axios.post("/api/v1/profile/general/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_PROFILE_CONTACT({ data }) {
      try {
        const res = await axios.post("/api/v1/profile/contact/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_PROFILE_PASSWORD({ data }) {
      try {
        const res = await axios.post("/api/v1/profile/password/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_DATA(id) {
      try {
        const res = await axios.get(`/api/v1/user/${id}`);
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_USER({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/user`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_USER({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/user/update`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_USER({ id_list }) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/user",
          data: {
            deletion_id: id_list,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EMAIL_REPORT({ start_date, end_date } = {}) {
      try {
        let params_list = {};
        if (start_date) {
          params_list.start_date = start_date;
        }
        if (end_date) {
          params_list.end_date = end_date;
        }
        const res = await axios.get("/api/v1/email_rapor", {
          params: params_list,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_EMAIL_REPORT({ start_date, end_date, params } = {}) {
      try {
        let params_list = {};
        if (start_date) {
          params_list.start_date = start_date;
        }
        if (end_date) {
          params_list.end_date = end_date;
        }
        if (params) {
          params_list = { ...params_list, ...params };
        }
        const res = await axios.get("/api/v1/email_rapor/userRapor", {
          params: params_list,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_EMAIL_ANSWERS({ start_date, end_date, page } = {}) {
      try {
        let params_list = {
          per_page: 16,
          page: page ? page : 1,
        };
        if (start_date) {
          params_list.start_date = start_date;
        }
        if (end_date) {
          params_list.end_date = end_date;
        }
        const res = await axios.get("/api/v1/email_answer", {
          params: params_list,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async SET_USER_EMAIL_ANSWER_STATUS(id) {
      try {
        const res = await axios.post("/api/v1/email_answer", {
          id,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_ROLE(id) {
      try {
        const res = await axios.get(`/api/v1/role`, {
          params: {
            id,
          },
        });
        if (this.user.data.id == id) {
          this.user.role = await res.data.stats.permission_data;
        }
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_USER_ROLE(body) {
      try {
        const res = await axios.put(`/api/v1/role`, body);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_TARGET(user_id) {
      try {
        const res = await axios.get(`/api/v1/user_goal`, {
          params: {
            user_id,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_USER_TARGET({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/user_goal`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_USER_TARGET(body) {
      try {
        const res = await axios.put(`/api/v1/user_goal`, body);
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_USER_TARGET(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/user_goal",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EMAIL_REPORT_TOP_5({ start_date, end_date } = {}) {
      try {
        let params_list = {};
        if (start_date) {
          params_list.start_date = start_date;
        }
        if (end_date) {
          params_list.end_date = end_date;
        }
        const res = await axios.get("/api/v1/email_rapor/userRaporTop5", {
          params: params_list,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_WEBSITE_CONTACT_FORM(id) {
      try {
        const res = await axios.get(`/api/website/contact/form/${id}`);
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_WEBSITE_CONTACT_FORM_ANSWERED_STATUS({ id, is_answered }) {
      try {
        const res = await axios.patch(`/api/website/contact/form/${id}/answered`, {
          is_answered,
        });
        return res.data.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DASHBOARD_GOAL_TARGET({ key, start_date, end_date, params } = {}) {
      try {
        const today = new Date().toISOString().split("T")[0];
        const lastDayOfMonth = format(endOfMonth(today), "yyyy-MM-dd");
        const firstDayOfMonth = format(startOfMonth(today), "yyyy-MM-dd");

        const res = await axios.get(`/api/v1/rapor/goalTarget`, {
          params: {
            key: key,
            start_date: key == "monthly" ? firstDayOfMonth : today,
            end_date: lastDayOfMonth,
            ...params,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_TRANSIT_DECLARATION_DOCUMENT_TYPES() {
      try {
        const res = await axios({
          method: "GET",
          url: `/api/v1/transit-declaration/document/type/list`,
        });
        this.transit_declaration_document_types = res.data.data;
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async GET_TRANSIT_DECLARATIONS() {
      try {
        const res = await axios({
          method: "GET",
          url: `/api/v1/transit-declaration/list`,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async GET_TRANSIT_DECLARATION(id) {
      try {
        const res = await axios({
          method: "GET",
          url: `/api/v1/transit-declaration/single/${id}`,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_TRANSIT_DECLARATION({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/transit-declaration/store`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_TRANSIT_DECLARATION({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/transit-declaration/update`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_TRANSIT_DECLARATION({ id_list }) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/transit-declaration/delete",
          data: {
            deletion_id: id_list,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_TRANSIT_DECLARATION_DOCUMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/document/type/store", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_TRANSIT_DECLARATION_DOCUMENT_TYPE({ data }) {
      try {
        const res = await axios.put("/api/v1/transit-declaration/document/type/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_TRANSIT_DECLARATION_DOCUMENT_TYPE({ data }) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/document/type/delete", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_TRANSIT_DECLARATION_DOCUMENT({ data }) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/document/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async ADD_EXTEND_TIME_ORDINO(ordino_document_id) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/extend-the-time", {
          id: ordino_document_id,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CHANGE_ORDINO_STATUS({ id, status }) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/update-status", {
          id,
          status,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_ACCOUNT_GROUP_CODE({ data }) {
      try {
        const res = await axios.post("/api/v1/account-group-code", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_ACCOUNT_GROUP_CODE({ data }) {
      try {
        const res = await axios.put("/api/v1/account-group-code", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_ACCOUNT_GROUP_CODE({ data }) {
      try {
        const res = await axios.delete("/api/v1/account-group-code", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_ACCOUNT_PLAN({ data }) {
      try {
        const res = await axios.post("/api/v1/accounting-plan", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_ACCOUNT_PLAN({ data }) {
      try {
        const res = await axios.put("/api/v1/accounting-plan", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_ACCOUNT_PLAN({ data }) {
      try {
        const res = await axios.delete("/api/v1/accounting-plan", {
          params: data,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_CAR(id) {
      try {
        const res = await axios.get(`/api/v1/car/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_CAR({ data }) {
      try {
        const res = await axios.post("/api/v1/car", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_CAR({ data }) {
      try {
        const res = await axios.put("/api/v1/car", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_CAR(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/car",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_REAL_LOAD(id) {
      try {
        const res = await axios.get(`/api/v1/load_transfer/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_REAL_LOAD({ data, id }) {
      try {
        const res = await axios.post(`/api/v1/load_transfer/${id}`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_REAL_LOAD_PACKAGE(id) {
      try {
        const res = await axios.delete(`/api/v1/load_transfer/load_transfer_package`, {
          params: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_REAL_LOAD_INVOICE_ITEM(id) {
      try {
        const res = await axios.delete(`/api/v1/load_transfer/load_transfer_invoice_item`, {
          params: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_ORDINO(id) {
      try {
        const res = await axios({
          method: "GET",
          url: `/api/v1/transit-declaration/ordino/single/${id}`,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_ORDINO({ data }) {
      try {
        const res = await axios.post("/api/v1/transit-declaration/ordino/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_ORDINO(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/transit-declaration/ordino/delete",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EXPEDITION(id) {
      try {
        const res = await axios.get(`/api/v1/expedition/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_EXPEDITION({ data }) {
      try {
        const res = await axios.post("/api/v1/expedition", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_EXPEDITION({ data }) {
      try {
        const res = await axios.put("/api/v1/expedition", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_EXPEDITION(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/expedition",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EXPEDITION_LOADS(id) {
      try {
        const res = await axios.get(`/api/v1/expedition_load_mapping/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async SAVE_EXPEDITION_LOAD({ data }) {
      try {
        const res = await axios.post(`/api/v1/expedition_load_mapping`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_EXPEDITION_LOAD(id) {
      try {
        const res = await axios.delete(`/api/v1/expedition_load_mapping`, {
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_DAILY_JOBS(user_id) {
      try {
        const res = await axios.get(`/api/v1/user_daily_report`, {
          params: {
            user_id,
            per_page: 10,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_DAILY_JOB(id) {
      try {
        const res = await axios.get(`/api/v1/user_daily_report/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_USER_DAILY_JOB({ data }) {
      try {
        const res = await axios({
          method: "POST",
          url: `/api/v1/user_daily_report`,
          data: data,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message ? error.response.data.message : useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_USER_DAILY_JOB(body) {
      try {
        const res = await axios.put(`/api/v1/user_daily_report`, body);
        return res.data;
      } catch (error) {
        toast.error(error.response.data.error ? error.response.data.error : useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_USER_DAILY_JOB(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/user_daily_report",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DAILY_JOBS_REPORT(params = {}) {
      try {
        const res = await axios.get(`/api/v1/user_daily_report/adminAll`, {
          params: params,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_TIMEOUT_OFFER_REPORT(params = {}) {
      try {
        const res = await axios.get(`/api/v1/user_rapor/loadCountUserDetail`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_ACCOUNT_EXPENSE_REPORT(params = {}) {
      try {
        const res = await axios.get(`/api/v1/report/account/expense-report`, {
          params: params,
        });
        return res.data.rowDatas;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DETAILED_ACCOUNT_EXPENSE_REPORT(params = {}) {
      try {
        const res = await axios.get(`/api/v1/report/account/detailed-expense`, {
          params: params,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_SINGLE_ACCOUNT_EXPENSE_REPORT(params = {}) {
      try {
        const res = await axios.get(`/api/v1/report/account/single-expense`, {
          params: params,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DESTINATIONS(params = {}) {
      try {
        const res = await axios.get("/api/v1/destination", {
          params: { ...params },
        });
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DESTINATION(id) {
      try {
        const res = await axios.get(`/api/v1/destination/${id}`);
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_DESTINATION({ data }) {
      try {
        const res = await axios.post("/api/v1/destination", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_DESTINATION({ data }) {
      try {
        const res = await axios.put(`/api/v1/destination/${data.id}`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_DESTINATION({ data }) {
      try {
        const res = await axios.delete(`/api/v1/destination/${data.id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_LOAD_TRANSFER_MOVEMENTS(params = {}) {
      try {
        const res = await axios.get("/api/v1/load_transfer_movement", {
          params: { ...params },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_LOAD_TRANSFER_MOVEMENT(id) {
      try {
        const res = await axios.get(`/api/v1/load_transfer_movement/${id}`);
        return res.data?.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EXPEDITION_MOVEMENTS(id) {
      try {
        const res = await axios.get(`/api/v1/expedition/${id}/movements`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_EXPEDITION_MOVEMENT({ id, data }) {
      try {
        const res = await axios.post(`/api/v1/expedition/${id}/movements`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_EXPEDITION_MOVEMENT({ expedition_id, movement_id }) {
      try {
        const res = await axios.delete(`/api/v1/expedition/${expedition_id}/movements/${movement_id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_LOAD_TRANSFER_MOVEMENT({ data }) {
      try {
        const res = await axios.post("/api/v1/load_transfer_movement", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD_TRANSFER_MOVEMENT({ data }) {
      try {
        const res = await axios.post(`/api/v1/load_transfer_movement/update`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_LOAD_TRANSFER_MOVEMENT(id) {
      try {
        const res = await axios.delete(`/api/v1/load_transfer_movement`, {
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_SYSTEM_STATISTICS_COUNT() {
      try {
        const res = await axios.get("/api/v1/system_statistics_count");
        this.system_statistics_count = res.data;
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DASHBOARD_DATA(params = {}) {
      try {
        const res = await axios.get(`/api/v1/dashboard-data`, {
          params: params,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_CURRENCIES() {
      try {
        const res = await axios.get(`/api/v1/currency`);
        this.currencies = res.data.data;
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_INVOICE(id) {
      try {
        const res = await axios.get(`/api/v1/invoice/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_INVOICE({ data }) {
      try {
        const res = await axios.post("/api/v1/invoice", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_INVOICE({ data }) {
      try {
        const res = await axios.post("/api/v1/invoice/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_INVOICE(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/invoice/delete",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async INVOICE_ACCEPT_REJECT({ invoice_id, status, reason }) {
      try {
        const res = await axios.post("/api/v1/invoice/accept-or-reject", {
          invoice_id: invoice_id,
          status: status,
          reason: reason,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message ? error.response.data.message : useGetFirstError(error.response.data.errors));
      }
    },
    async GET_INVOICE_INBOX_PDF_VIEW({ id }) {
      try {
        const res = await axios.get(`/api/v1/invoice/pdf-view/inbox`, {
          params: {
            invoice_id: id,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message ? error.response.data.message : useGetFirstError(error.response.data.errors));
      }
    },
    async GET_INVOICE_OUTBOX_PDF_VIEW({ id }) {
      try {
        const res = await axios.get(`/api/v1/invoice/pdf-view/outbox`, {
          params: {
            invoice_id: id,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message ? error.response.data.message : useGetFirstError(error.response.data.errors));
      }
    },
    async GET_INVOICE_FOOTERS({ invoice_id }) {
      try {
        const res = await axios.get(`/api/v1/invoice/footer`, {
          params: {
            invoice_id: invoice_id,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message ? error.response.data.message : useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_INVOICE_FOOTER({ data }) {
      try {
        const res = await axios.post("/api/v1/invoice/footer", data);
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
    async UPDATE_INVOICE_FOOTER({ data }) {
      try {
        const res = await axios.post("/api/v1/invoice/footer/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_INVOICE_FOOTER(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/invoice/footer",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_DASHBOARD_EXPENSE_DATA(params = {}) {
      try {
        const res = await axios.get(`/api/v1/report/account/total-expense`, {
          params: params,
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async UPDATE_LOAD_FILE({ data }) {
      try {
        const res = await axios.post(`/api/v1/load/file/upload`, data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_WORKING_TRACKING(id) {
      try {
        const res = await axios.get(`/api/v1/working-tracking/${id}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_USER_WORKING_TRACKING({ id, date_range }) {
      try {
        const res = await axios.get(`/api/v1/working-tracking/user/${id}${date_range ? `?date_range=${date_range}` : ""}`);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async CREATE_WORKING_TRACKING({ data }) {
      try {
        const res = await axios.post("/api/v1/working-tracking", data);
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
    async UPDATE_WORKING_TRACKING({ data }) {
      try {
        const res = await axios.post("/api/v1/working-tracking/update", data);
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async DELETE_WORKING_TRACKING(id) {
      try {
        const res = await axios({
          method: "DELETE",
          url: "/api/v1/working-tracking/delete",
          data: {
            deletion_id: [id],
          },
        });
        return res.data;
      } catch (error) {
        toast.error(useGetFirstError(error.response.data.errors));
      }
    },
    async GET_EXCHANGE_RATE({ currency_code, date }) {
      try {
        const res = await axios.get("/api/v1/currency/get-exchange-rate", {
          params: {
            currency_code: currency_code,
            date: date,
          },
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
    async CREATE_DRAFT_INVOICE({ invoice_id, currency_code, exchange_date, exchange_price }) {
      try {
        const res = await axios.post("/api/v1/invoice/draft/send", {
          invoice_id: invoice_id,
          currency_type: currency_code,
          exchange_date: exchange_date,
          exchange_price: exchange_price,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
    async INVOICE_DRAFT_APPROVE({ invoice_id }) {
      try {
        const res = await axios.post("/api/v1/invoice/draft/approve", {
          invoice_id: invoice_id,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
    async INVOICE_DRAFT_REJECT({ invoice_id }) {
      try {
        const res = await axios.post("/api/v1/invoice/draft/cancel", {
          invoice_id: invoice_id,
        });
        return res.data;
      } catch (error) {
        toast.error(error.response.data.message || "Error");
      }
    },
  },
});
