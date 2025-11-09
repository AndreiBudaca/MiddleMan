import { Box, Typography } from "@mui/material";
import { useEffect } from "react";
import ClientTreeView, {
  type TreeNode,
} from "~/components/dashboard/clientTreeView";
import { getClients } from "~/services/clientService";

export function meta() {
  return [{ title: "MiddleMan" }];
}

export default function Dashboard() {

  useEffect(() => {
    const fetchClients = async () => {
      await getClients();
     };
    fetchClients();
  })

  const clients: TreeNode[] = [
    {
      label: "Public",
      children: [
        { label: "Meta client" },
        {
          label: "Beta client",
          children: [{ label: "Meta method" }, { label: "Alpha method" }],
        },
      ],
    },
  ];

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

      <Box
        marginTop="30px"
        display="flex"
        width="100%"
        justifyContent="center"
        alignItems="center"
      >
        <ClientTreeView nodes={clients} />
      </Box>
    </>
  );
}
