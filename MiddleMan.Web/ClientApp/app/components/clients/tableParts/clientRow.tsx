import { IconButton, TableCell, TableRow } from "@mui/material";
import type { Client } from "~/services/clients/contracts/client";
import DeleteIcon from '@mui/icons-material/Delete';
import VpnKeyIcon from '@mui/icons-material/VpnKey';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';

export interface ClientRowProps {
  client: Client;
}

export function ClientRow({ client }: ClientRowProps) {
  return (
    <TableRow
      key={client.name}
      sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
    >
      <TableCell component="th" scope="row">
        {client.name}
      </TableCell>
      <TableCell align="right">
        {client.isConnected ? "online" : "offline"}
      </TableCell>
      <TableCell align="right">
        {client.lastConnectedAt?.toLocaleTimeString() ?? "never"}
      </TableCell>
      <TableCell align="right">
        {client.signature ?? "not registered"}
      </TableCell>
      <TableCell align="right">{client.tokenHash ?? "no token"}</TableCell>
      <TableCell align="right">
        <IconButton onClick={() => {}}>
            <ContentCopyIcon/>
        </IconButton>
        <IconButton onClick={() => {}}>
            <VpnKeyIcon/>
        </IconButton>
        <IconButton onClick={() => {}}>
            <DeleteIcon/>
        </IconButton>
      </TableCell>
    </TableRow>
  );
}
