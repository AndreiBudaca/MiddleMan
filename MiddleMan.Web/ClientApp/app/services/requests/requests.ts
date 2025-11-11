import { env } from "~/environment";
import { requestObserver } from "./requestEvents";

export async function get(url: string): Promise<Response | null> {
    const response = await fetch(url, {
        method: 'GET',
    });

    return handleResponse(response);
}

export async function post(url: string, data: any | null | undefined): Promise<Response | null> {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(data) 
    });

    return handleResponse(response);
}

async function handleResponse(response: Response): Promise<Response | null> {
    if (response.status == 401) {
        authenticate();
        return null;
    } else if (!response.ok) {
        await handleFailure(response);
        return null;
    }
    
    return response;
}

async function handleFailure(response: Response) {
    let error = "";
    try{
        error = await response.text();
    } catch (e) {
        error = "Unknown error occurred";
    }

    requestObserver.publish(
    {
        status: response.status,
        message: error,
    });
}

function authenticate() {
    window.location.replace(`${env.API_BASE_URL}/account/login`);
}