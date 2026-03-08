import { defineConfig } from "vitepress";
import apiSidebar from "../api/sidebar.mjs";

const guideItems = [
  { text: "Overview", link: "/" },
  { text: "Getting Started", link: "/articles/getting-started" },
  { text: "Selection Strategies", link: "/articles/selection-strategies" },
  { text: "Migrate from v1", link: "/articles/migrating-from-v1" }
];

export default defineConfig({
  title: "RoyalApps External Apps",
  description: "Managed WinForms host control for embedding windows from external processes.",
  base: "/royalapps-community-externalapps/",
  cleanUrls: true,
  themeConfig: {
    logo: "/assets/RoyalApps_1024.png",
    nav: [
      { text: "Guide", link: "/articles/getting-started" },
      { text: "API", link: "/api/" },
      { text: "GitHub", link: "https://github.com/royalapplications/royalapps-community-externalapps" }
    ],
    sidebar: {
      "/articles/": [
        {
          text: "Guide",
          items: guideItems
        },
        {
          text: "API",
          items: apiSidebar
        }
      ],
      "/api/": [
        {
          text: "Guide",
          items: guideItems
        },
        {
          text: "API",
          items: apiSidebar
        }
      ]
    },
    socialLinks: [
      { icon: "github", link: "https://github.com/royalapplications/royalapps-community-externalapps" }
    ],
    search: {
      provider: "local"
    },
    footer: {
      message: "MIT Licensed",
      copyright: "Copyright Royal Apps GmbH"
    }
  }
});
