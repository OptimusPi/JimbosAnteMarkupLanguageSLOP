import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// Aspire hands the port in through PORT.
//
// /agui is proxied to the MCP server so the agent panel calls same-origin and CORS never comes
// into it. Under Aspire the server's URL arrives as service discovery config; standalone it is
// the launch profile's port. MOTELY_MCP_URL overrides both.
const mcpUrl =
  process.env.services__mcp__http__0 ?? process.env.MOTELY_MCP_URL ?? "http://localhost:5200";

export default defineConfig({
  plugins: [react()],
  resolve: {
    dedupe: ["react", "react-dom", "motely-wasm"],
  },
  // jaml-ui finds its sprite sheets beside its own files. Pre-bundled for the dev server, those
  // paths point into .vite/deps, where no image is, and every card draws blank. Served as it
  // ships, the sheets load.
  optimizeDeps: { exclude: ["jaml-ui"] },
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    proxy: { "/agui": { target: mcpUrl } },
  },
  build: {
    target: "esnext",
    // .NET's JavaScript probes for Node modules it never uses in a browser.
    rollupOptions: { external: ["process", "module"] },
  },
});
