'use server'

import { Auction, PagedResult } from "@/types";

export async function getData(query: string) : Promise<PagedResult<Auction>> {

    var url = `http://localhost:6001/search${query}`;
    //console.log(url);
    const res = await fetch(url);

    if (!res.ok) throw Error('Failed to fetch the data.');

    return res.json();    
}