﻿using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

public class Island
{
    //-------------Data---------------------------
    int relaxation;//relaxation times
    int width;//screen width 
    int hight;//screen hight
    int num_of_centers;//number of centers
    public HashSet<IslandTile> ocean = new HashSet<IslandTile>();//store ocean tiles
    public HashSet<IslandTile> land = new HashSet<IslandTile>();//store land tiles
    public HashSet<IslandTileCorner> shore = new HashSet<IslandTileCorner>();//store corners in shore
    public HashSet<IslandTileCorner> totalcorners = new HashSet<IslandTileCorner>();//total corners
    public Dictionary<Vector, IslandTile> Tiles = new Dictionary<Vector, IslandTile>();//store all tiles
    public List<Vector> centers = new List<Vector>();//stores all tiles' positions
    public List<Vector> landcenters = new List<Vector>();//stores land tiles' postions
    //class constractor
    public Island(int w,int h,int r,int num)
    {
        width = w;
        hight = h;
        relaxation = r;
        num_of_centers = num;
        centers = random_centers(width, hight, num_of_centers);
        VoronoiGraph vg = Fortune.ComputeVoronoiGraph(centers);//run voronoi diagram algorithm
        for (int i = 0; i < centers.Count; i++)//Initialize and store IslandTiles
        {
            Tiles[centers[i]] = new IslandTile(centers[i], vg,width,hight);
        }
        //call improveRandomPoints function "relaxation" times
        for (int re = 0; re < relaxation; re++)
        {
            centers = improveRandomPoints(Tiles, centers);
            VoronoiGraph vGraph = Fortune.ComputeVoronoiGraph(centers);
            Tiles = new Dictionary<Vector, IslandTile>();
            for (int j = 0; j < centers.Count; j++)
            {
                Tiles[centers[j]] = new IslandTile(centers[j], vGraph,width,hight);
            }
        }
            foreach (var item in Tiles.Values)
            {
                if (item.center.data[0] < (width / 10) || item.center.data[0] > (width - width / 10) ||
                    item.center.data[1] < (width / 10) || item.center.data[1] > (width - width / 10))
                {
                    item.iswater = true;
                    item.elevation = 0;
                    foreach (var c in item.corners)
                    {
                        c.elevation = 0;
                        // totalcorners[c.position] = c;
                        //water.Add(c);
                    }
                    ocean.Add(item);
                }
                else
                    land.Add(item);

            }
        //spreading ocean area
        int waterspreadcount = 0;
        foreach (var item in Tiles.Values)
        {
            if (!item.iswater)
            {
                foreach (var i in item.neighbors)
                {
                    if (Tiles[i].iswater)
                    {
                        item.iswater = true;
                        item.elevation = 0;
                        foreach (var c in item.corners)
                        {
                            c.elevation = 0;
                            //totalcorners[c.position] = c;
                            //water.Add(c);
                        }
                        ocean.Add(item);
                        land.Remove(item);
                        waterspreadcount++;

                    }
                    if (waterspreadcount > (num_of_centers / 3))
                        break;
                }
            }
            if (waterspreadcount > (num_of_centers / 3))
                break;
        }
        //remove one tile island
        foreach (var item in Tiles.Values)
        {
            float sum_of_elevation = 0;
            foreach (var c in item.corners)
            {
                sum_of_elevation += c.elevation;
            }
            if (sum_of_elevation == 0)
            {
                item.iswater = true;
                item.elevation = 0;
                ocean.Add(item);
                land.Remove(item);
            }
        }
        //-----calculate coastline------------------------
        foreach (var item in land)
        {
            foreach (var c in item.corners)
                if (c.elevation == 0)
                {
                    shore.Add(c);
                    item.isshore = true;
                }
        }
        //calculate elevation for corners
        foreach (var t in Tiles.Values)
        {
            if (!t.iswater)
            {
                foreach (var c in t.corners)
                {
                    foreach (var s in shore)
                    {
                        float elevation = (float)Math.Sqrt(Math.Pow((c.position - s.position).data[0], 2) + Math.Pow(
                            (c.position - s.position).data[1], 2));
                        if (c.elevation > elevation)
                            c.elevation = elevation;
                    }
                   // totalcorners[c.position] = c;
                }

            }
        }
        //store total corners
        foreach(var item in Tiles.Values)
        {
            foreach (var c in item.corners)
                totalcorners.Add(c);
        }
        //landcenters
        foreach(var item in land)
        {
            landcenters.Add(item.center);

        }
            
        //from now on, all data of a tile are generated. 
        //next step is to the tile of an arbitrary pixel

    }
    
    //-------------Methods-----------------------------
    //----------------------1.get initial random points --------------------
    public List<Vector> random_centers(int width, int hight, int num_of_centers)
    {
        Random rnd = new Random();
        HashSet<int> points = new HashSet<int>();
        List<Vector> poi = new List<Vector>();
        while (points.Count < num_of_centers)
        {
            points.Add(rnd.Next(0, width * hight - 1));
        }
        
        foreach (int item in points)
        {
            int rows = item / width;
            int cols = item % width;
            //System.Console.WriteLine(rows);
            poi.Add(new Vector(rows, cols));//get row and col indexs

        }
        return poi;

    }

    //-----------2.improveRandomPoints(use the centroid of corners to replace inital centers)---
    public List<Vector> improveRandomPoints(Dictionary<Vector, IslandTile> Tiles, List<Vector> c)
    {
        List<Vector> improvepoint_list = new List<Vector>();
        for (int i = 0; i < c.Count; i++)
        {
            Vector improvepoint = new Vector(0, 0);//the point in the center of a tile
            foreach (IslandTileCorner cor in Tiles[c[i]].corners)
            {
                improvepoint += cor.position;//add all corners together and then calculate the centriod 
            }
            improvepoint.data[0] = (int)(improvepoint.data[0] / Tiles[c[i]].corners.Count);
            improvepoint.data[1] = (int)(improvepoint.data[1] / Tiles[c[i]].corners.Count);

            improvepoint_list.Add(improvepoint);
        }
        return improvepoint_list;
    }
    //----------------------------3.generate rivers-----------------------------
    //this function will return a list of rivers,and the num of river is decided by you in your main function
    public List<River> generationofRivers(int num)
    {
        List<River> totalriver = new List<River>();
        Random rnd = new Random();
        HashSet<IslandTileCorner> startpoints = new HashSet<IslandTileCorner>();
        while(startpoints.Count<num)
        {
            int index=rnd.Next(0, landcenters.Count);
            
            foreach (var c in Tiles[landcenters[index]].corners)
            {
              if (c.elevation != 0)
              {
                startpoints.Add(c);
               //System.Console.WriteLine("haha");
                break;
              }
            }    
        }
        foreach (var item in startpoints)
        {
            River r = new River();
            r.generateriver(item);
            totalriver.Add(r);
        }
        return totalriver;
            
            
    }


}