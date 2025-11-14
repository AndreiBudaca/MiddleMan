import { TableCell, TableHead, TableRow } from "@mui/material";

export function ClientHeader() {
  return (
    <TableHead>
      <TableRow>
        <TableCell>Name</TableCell>
        <TableCell align="right">Status</TableCell>
        <TableCell align="right">Last connection</TableCell>
        <TableCell align="right">Methods hash</TableCell>
        <TableCell align="right">Token hash</TableCell>
        <TableCell align="right">Actions</TableCell>
      </TableRow>
    </TableHead>
  );
}
