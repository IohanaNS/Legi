import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import ptBR from "./locales/pt-BR.json";
import en from "./locales/en.json";

i18n.use(initReactI18next).init({
  resources: {
    "pt-BR": { translation: ptBR },
    en: { translation: en },
  },
  lng: "pt-BR",             // idioma padrão
  fallbackLng: "pt-BR",     // se uma chave não existir no idioma atual, usa este
  interpolation: {
    escapeValue: false,      // React já faz XSS protection, não precisa escapar
  },
});

export default i18n;