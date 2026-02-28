import type { NextConfig } from "next";
import path from "path";

const nextConfig: NextConfig = {
  turbopack: {
    root: path.join(__dirname),
  },
  // Allow development access from other devices on the network
  allowedDevOrigins: [
    "localhost",
    "127.0.0.1",
    "192.168.0.104",      // Local network IP
    "*.local",             // Local network domains
  ],
};

export default nextConfig;
