import { type RouteConfig, index, route } from "@react-router/dev/routes";

export default [
  index("routes/dashboard.tsx"),
  route("/clients", "routes/clients.tsx")
] satisfies RouteConfig;
