/** @type {import('next').NextConfig} */
const nextConfig = {
    images : {
        remotePatterns :[{
                hostname: 'cdn.pixabay.com'
            },  {
                    hostname: 'encrypted-tbn0.gstatic.com'
            }
        ]
    }
};

export default nextConfig;
