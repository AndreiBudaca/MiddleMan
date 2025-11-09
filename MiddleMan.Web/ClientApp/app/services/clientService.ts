import { env } from "~/environment";
import { get } from "./requests";

export async function getClients() {
    console.log("Fetching clients from:", env.API_BASE_URL);
    const result = await get(`${env.API_BASE_URL}/WebSockets`);
    const data = await result?.json();
    console.log(data);
}