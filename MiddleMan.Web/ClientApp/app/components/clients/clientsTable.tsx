import { useEffect, useState } from "react";
import type { Client } from "~/services/clients/contracts/client";
import { addNewClient, getClients } from "~/services/clients/clientService";
import {
  Box,
  Button,
  createTheme,
  Paper,
  Table,
  TableBody,
  TableContainer,
  ThemeProvider,
  Typography,
} from "@mui/material";
import { ClientRow } from "./tableParts/clientRow";
import { ClientHeader } from "./tableParts/clientHeader";
import { NewClientRow } from "./tableParts/newClientRow";

export default function ClientsTable() {
  const [clients, setClients] = useState<Client[]>([]);
  const [newClients, setNewClients] = useState<number[]>([]);

  const darkTheme = createTheme({
    palette: {
      mode: "dark",
    },
  });

  useEffect(() => {
    const fetchClients = async () => {
      const result = await getClients();
      setClients(result);
    };
    fetchClients();
  }, []);

  const addClient = () => {
    if (newClients.length === 0) {
      setNewClients([0]);
    } else {
      const lastClientKey = newClients[newClients.length - 1];
      setNewClients([...newClients, lastClientKey + 1]);
    }
  };

  const saveNewClient = async (key: number, name: string) => {
    const newClient = await addNewClient({ name: name });
    if (newClient) {
      setClients([...clients, newClient]);
      discardNewClient(key);
    }
  };

  const discardNewClient = (key: number) => {
    setNewClients(newClients.filter((newClient) => newClient != key));
  };

  return (
    <Box
      marginTop="50px"
      display="flex"
      flexDirection="column"
      alignItems="flex-end"
      width="100%"
      gap="20px"
      flexWrap="wrap"
    >
      <ThemeProvider theme={darkTheme}>
        <Box
          display="flex"
          width="100%"
          justifyContent="space-between"
          alignItems="flex-end"
        >
          <Typography variant="h5">Your clients</Typography>
          <Button variant="contained" onClick={addClient}>
            Add
          </Button>
        </Box>
        <TableContainer component={Paper}>
          <Table sx={{ minWidth: 650 }} aria-label="simple table">
            <ClientHeader />
            <TableBody>
              {clients.map((client) => (
                <ClientRow client={client} />
              ))}
              {newClients.map((key) => (
                <NewClientRow
                  key={key}
                  clientKey={key}
                  onSave={saveNewClient}
                  onRemove={discardNewClient}
                />
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </ThemeProvider>
    </Box>
  );
}
