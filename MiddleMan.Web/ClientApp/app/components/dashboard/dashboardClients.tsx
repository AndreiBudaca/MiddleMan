import { useEffect, useState } from "react";
import type { ClientWithMethods } from "~/contracts/client";
import { getClientsWithMethods } from "~/services/clients/clientService";
import ClientTreeView from "./clientTreeView";
import { Box } from "@mui/material";
import type { ClientMethod } from "~/contracts/clientMethods";
import MethodsView from "./methodsView";

export default function DashboardClients() {
  const [clients, setClients] = useState<ClientWithMethods[]>([]);
  const [selectedClient, setSelectedClient] =
    useState<ClientWithMethods | null>(null);
  const [selectedMethod, setSelectedMethod] = useState<ClientMethod | null>(
    null
  );

  const onSelectedMethodChange = (path: string) => {
    const pathParts = path.split("/");
    if (pathParts.length != 2) return;

    const eligibleClients = clients.filter((c) => c.name == pathParts[0]);
    if (
      !eligibleClients ||
      !eligibleClients.length ||
      eligibleClients.length < 1
    )
      return;

    const eligibleMethods = eligibleClients[0].methods.filter(
      (m) => m.name == pathParts[1]
    );
    if (
      !eligibleMethods ||
      !eligibleMethods.length ||
      eligibleMethods.length < 1
    )
      return;

    setSelectedClient(eligibleClients[0]);
    setSelectedMethod(eligibleMethods[0]);
  };

  useEffect(() => {
    const fetchClients = async () => {
      const result = await getClientsWithMethods();
      setClients(result);
    };
    fetchClients();
  }, []);

  return (
    <Box
      marginTop="50px"
      display="flex"
      width="100%"
      gap="20px"
      flexWrap="wrap"
    >
      <ClientTreeView
        clients={clients}
        onNodeSelected={onSelectedMethodChange}
      />
      <MethodsView
        client={selectedClient?.name}
        method={selectedMethod}
        isCallable={selectedClient?.isConnected ?? false}
      />
    </Box>
  );
}
