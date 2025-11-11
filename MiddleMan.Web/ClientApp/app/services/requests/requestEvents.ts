export interface RequestEvent
{
    status: number;
    message: string;
}

class RequestEventObserver
{
    private subscribers: Array<(event: RequestEvent) => void> = [];

    public publish(event: RequestEvent)
    {
        for (const subscriber of this.subscribers)
        {
            subscriber(event);
        }
    }
    
    public subscribe(callback: (event: RequestEvent) => void)
    {
        this.subscribers.push(callback);
    }
}

export const requestObserver = new RequestEventObserver();