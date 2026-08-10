import { reactive, ref } from "vue";
import { toast } from "vue-sonner";
import axios from "axios";
import { useDataStore } from "@/stores/data_store.js";
import { useGeneralStore } from "@/stores/general_store.js";

export const useDatatable = (config) => {
  const state = reactive({
    items: [],
    meta: null,
    loading: false,
    delete_loading: false,
    sortData: null,
    searchQuery: "",
    selectedRows: [],
    routes: {
      get: {
        url: config.getUrl,
        params: {
          per_page: config.perPage || 10,
          page: 1,
          ...config.defaultParams,
        },
        method: "GET",
      },
      delete: config.deleteUrl
        ? {
            url: config.deleteUrl,
            method: "DELETE",
          }
        : null,
      view: config.viewUrl
        ? {
            url: config.viewUrl,
            method: "GET",
          }
        : null,
    },
  });

  const dialogState = reactive({
    deleteVisible: false,
    itemToDelete: null,
  });

  const fetchData = async (params = {}) => {
    try {
      state.loading = true;
      const fetchParams = {
        ...state.routes.get.params,
        ...params,
      };

      if (state.sortData) {
        fetchParams.sort = state.sortData.sortField;
        fetchParams.order = state.sortData.sortOrder;
      }

      if (state.searchQuery) {
        fetchParams.search = state.searchQuery;
      }

      const response = await axios.get(state.routes.get.url, { params: fetchParams });
      if (config.responseDataKey) {
        const keys = config.responseDataKey.split(".");
        let data = response.data;

        for (const key of keys) {
          data = data[key];
        }

        state.items = data.data;
        state.meta = data;
      } else {
        state.items = response.data.data.data;
        state.meta = response.data.data;
      }
    } catch (error) {
      console.error("Error fetching data:", error);
    } finally {
      state.loading = false;
    }
  };

  const handlePageChange = (event) => {
    state.routes.get.params.page = event.page + 1;
    fetchData();
  };

  const handleSort = (event) => {
    state.sortData = event;
    fetchData();
  };

  const handleSearch = (() => {
    let debounceTimeout;
    return (value) => {
      state.searchQuery = value;
      clearTimeout(debounceTimeout);
      debounceTimeout = setTimeout(() => {
        state.routes.get.params.page = 1;
        fetchData();
      }, 500);
    };
  })();

  const handleDelete = async () => {
    if (!state.routes.delete || !dialogState.itemToDelete) return;

    state.delete_loading = true;
    try {
      await axios({
        method: "DELETE",
        url: state.routes.delete.url,
        data: {
          deletion_id: [dialogState.itemToDelete.id],
        },
      });
      closeDeleteDialog();
      toast.success("Başarıyla silindi");
      await fetchData({ page: state.meta.current_page });
      return true;
    } catch (error) {
      console.error("Error deleting item:", error);
      return false;
    } finally {
      state.delete_loading = false;
    }
  };

  const showDeleteDialog = (item) => {
    dialogState.itemToDelete = item;
    dialogState.deleteVisible = true;
  };

  const closeDeleteDialog = () => {
    dialogState.deleteVisible = false;
    dialogState.itemToDelete = null;
  };

  const handleView = (id) => {
    if (!state.routes.view) return;
    window.location.href = `${state.routes.view.url}/${id}`;
  };

  const refresh = () => {
    fetchData({ page: state.routes.get.params.page });
  };

  const setParams = (params) => {
    state.routes.get.params = {
      ...state.routes.get.params,
      ...params,
    };
  };

  const removeParams = (params) => {
    for (const key in params) {
      delete state.routes.get.params[key];
    }
  };

  const clear = () => {
    state.items = [];
    state.meta = null;
    state.loading = false;
    state.sortData = null;
    state.searchQuery = "";
    state.selectedRows = [];
  };

  return {
    state,
    dialogState,
    fetchData,
    handlePageChange,
    handleSort,
    handleSearch,
    handleDelete,
    showDeleteDialog,
    closeDeleteDialog,
    handleView,
    refresh,
    setParams,
    removeParams,
    clear,
  };
};

export const useGetFirstError = (data) => {
  for (const key in data) {
    if (data[key] && data[key].length > 0) {
      return data[key][0];
    }
  }
  return "Error";
};

export const useGetIsoDate = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  let formattedDate = `${year}-${month}-${day}`;

  return formattedDate;
};

export const useGetIsoDateWithTime = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  let formattedDate = `${year}-${month}-${day}`;

  // Saat bilgisi sıfır değilse saat:dk:sn ekle
  const hasTime = date.getHours() !== 0 || date.getMinutes() !== 0 || date.getSeconds() !== 0;

  if (hasTime) {
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const seconds = String(date.getSeconds()).padStart(2, "0");
    formattedDate += ` ${hours}:${minutes}:${seconds}`;
  }

  return formattedDate;
};

export const usePermissionStatus = (permission_slug) => {
  const DataStore = useDataStore();
  let selected_roles = DataStore.user.role.find((item) => item.permission_page_slug == permission_slug);
  return selected_roles
    ? selected_roles
    : {
        create: false,
        read: false,
        update: false,
        delete: false,
      };
};

export const useCurrencyConverter = ({ current_currency_code, new_currency_code, price }) => {
  const DataStore = useDataStore();

  if (current_currency_code === new_currency_code) {
    return price;
  }

  const currencies = DataStore.currencies;

  if (!currencies || currencies.length === 0) {
    console.error("Currency data not available");
    return price;
  }

  const currentCurrency = currencies.find((c) => c.code == current_currency_code);
  const newCurrency = currencies.find((c) => c.code == new_currency_code);

  if (!currentCurrency || !newCurrency) {
    console.error("One or both currencies not found");
    return price;
  }

  const currentRate = currentCurrency.exchange_rate?.forex_buying || 1;
  const newRate = newCurrency.exchange_rate?.forex_buying || 1;

  if (current_currency_code === "TRY") {
    return (price / newRate).toFixed(2);
  } else if (new_currency_code === "TRY") {
    return (price * currentRate).toFixed(2);
  } else {
    const tryValue = price * currentRate;
    return (tryValue / newRate).toFixed(2);
  }
};

export const useMoneyFormat = (price) => {
  return Number(price).toLocaleString("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

export const useFormatMinutes = ({ minutes, short = false }) => {
  if (!minutes || isNaN(minutes)) {
    return `0 ${short ? "dk" : "dakika"}`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (hours === 0) {
    return `${remainingMinutes} ${short ? "dk" : "dakika"}`;
  } else if (remainingMinutes === 0) {
    return `${hours} ${short ? "sa" : "saat"}`;
  } else {
    return `${hours} ${short ? "sa" : "saat"} ${remainingMinutes} ${short ? "dk" : "dakika"}`;
  }
};

export const useVisibleUser = (user_ids) => {
  const GeneralStore = useGeneralStore();
  if (user_ids.includes(GeneralStore.user_data.id)) {
    return true;
  }
  return false;
};
