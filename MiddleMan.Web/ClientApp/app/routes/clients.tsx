import ClientsTable from "~/components/clients/clientsTable";

export function meta() {
  return [
    { title: "MiddleMan" },
  ];
}

export default function Clients() {
  return (
    <ClientsTable />
  );
}