import { Box, Typography } from "@mui/material";
import DashboardClients from "~/components/dashboard/dashboardClients";

export function meta() {
  return [{ title: "MiddleMan" }];
}

export default function Dashboard() {

  return (
    <>
      <Box
        marginTop="30px"
        display="flex"
        width="100%"
        justifyContent="center"
        alignItems="center"
      >
        <Typography variant="h2">Dashboard</Typography>
      </Box>
      <DashboardClients />
    </>
  );
}
