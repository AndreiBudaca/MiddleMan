import { TableCell, TableHead, TableRow, Typography } from "@mui/material";

export function ClientHeader() {
  const columns = [
    "Name",
    "Status",
    "Methods hash",
    "Token hash",
    "Actions",
  ];

  return (
    <TableHead>
      <TableRow>
        {columns.map((column) => (
          <TableCell>
            <Typography variant="body1" fontWeight="bold">{column}</Typography>
          </TableCell>
        ))}
      </TableRow>
    </TableHead>
  );
}
