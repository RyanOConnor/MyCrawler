package com.android.ryan.cloudcrawlerclient;

import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.os.AsyncTask;
import android.support.v4.app.FragmentManager;
import android.support.v7.app.ActionBarActivity;
import android.app.Activity;
import android.support.v7.app.ActionBar;
import android.support.v4.app.Fragment;
import android.support.v4.app.ActionBarDrawerToggle;
import android.support.v4.view.GravityCompat;
import android.support.v4.widget.DrawerLayout;
import android.content.SharedPreferences;
import android.content.res.Configuration;
import android.os.Bundle;
import android.preference.PreferenceManager;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.ExpandableListView;
import android.widget.ListView;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
/**
 * Created by Ryan on 5/3/2015.
 */
public class NavigationDrawerFragment extends Fragment {

    private static final String STATE_SELECTED_GROUP = "selected_navigation_drawer_group";
    private static final String STATE_SELECTED_ITEM = "selected_navigation_drawer_item";
    private static final String PREF_USER_LEARNED_DRAWER = "navigation_drawer_learned";

    private ActionBarDrawerToggle mDrawerToggle;

    private DrawerLayout mDrawerLayout;
    private ExpandableListView mDrawerListView;
    private View mFragmentContainerView;

    private int mCurrentSelectedGroup = 0;
    private int mCurrentSelectedItem = 0;
    private boolean mFromSavedInstanceState;
    private boolean mUserLearnedDrawer;

    public static ExpandableListAdapter listAdapter;
    private List<String> dataHeaders;
    private HashMap<String, List<String>> dataChildren;

    public NavigationDrawerFragment() {
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        SharedPreferences sp = PreferenceManager.getDefaultSharedPreferences(getActivity());
        mUserLearnedDrawer = sp.getBoolean(PREF_USER_LEARNED_DRAWER, false);
        if (savedInstanceState != null) {
            mCurrentSelectedGroup = savedInstanceState.getInt(STATE_SELECTED_GROUP);
            mCurrentSelectedItem = savedInstanceState.getInt(STATE_SELECTED_ITEM);
            mFromSavedInstanceState = true;
        }
        select(mCurrentSelectedGroup, mCurrentSelectedItem);
    }

    @Override
    public void onActivityCreated(Bundle savedInstanceState) {
        super.onActivityCreated(savedInstanceState);
        prepareListData();
        listAdapter = new ExpandableListAdapter(mFragmentContainerView.getContext(), dataHeaders, dataChildren);
        mDrawerListView.setAdapter(listAdapter);
        mDrawerListView.setOnGroupClickListener(new ExpandableListView.OnGroupClickListener() {
            @Override
            public boolean onGroupClick(ExpandableListView parent, View v, int groupPosition, long id) {
                return select(groupPosition, -1);
            }
        });
        mDrawerListView.setOnChildClickListener(new ExpandableListView.OnChildClickListener() {
            @Override
            public boolean onChildClick(ExpandableListView parent, View v, int groupPosition, int childPosition, long id) {
                return select(groupPosition, childPosition);
            }
        });
        mDrawerListView.setOnItemLongClickListener(new ExpandableListView.OnItemLongClickListener() {
            @Override
            public boolean onItemLongClick(AdapterView<?> parent, View v, int groupPosition, long id) {
                if (ExpandableListView.getPackedPositionType(id) == ExpandableListView.PACKED_POSITION_TYPE_CHILD) {
                    int groupPos = ExpandableListView.getPackedPositionGroup(id);
                    int childPos = ExpandableListView.getPackedPositionChild(id);
                    if (groupPos == 1) {
                        String feedTitle = (String) listAdapter.getChild(1, childPos);
                        NavigationDrawerDialogs.showModifyFeedDialog(childPos, feedTitle);
                    }
                    return true;
                } else {
                    return false;
                }
            }
        });
        setHasOptionsMenu(true);
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
                             Bundle savedInstanceState) {
        mDrawerListView = (ExpandableListView) inflater.inflate(
                R.layout.fragment_navigation_drawer, container, false);
        mDrawerListView.setItemChecked(mCurrentSelectedGroup, true);
        return mDrawerListView;
    }

    public void prepareListData(){
        dataHeaders = new ArrayList<>();
        dataChildren = new HashMap<String, List<String>>();

        dataHeaders.add(getString(R.string.title_Add));
        dataHeaders.add(getString(R.string.title_Feeds));
        dataHeaders.add(getString(R.string.title_Account));
        dataHeaders.add(getString(R.string.title_Logout));

        List<String> feeds = StorageManager.instance().readFeedTitles(MainActivity.mContext);
        List<String> account = new ArrayList<String>();
        account.add("Delete Account");
        account.add("Change Password");
        dataChildren.put(dataHeaders.get(1), feeds);
        dataChildren.put(dataHeaders.get(2), account);
    }

    public boolean select(int group, int child){
        boolean ret = true;
        Fragment fragment = null;
        mCurrentSelectedGroup = group;
        if (mDrawerListView != null) {
            mDrawerListView.setItemChecked(group, true);
        }
        if ( mDrawerLayout != null && ( group != 1 && group != 2 ) ) {
            mDrawerLayout.closeDrawer(mFragmentContainerView);
        }
        switch(group) {
            case 0:
                fragment = AddFeedFragment.newInstance(group + 1);
                break;
            case 1:
                fragment = getFeedFragment(child);
                ret = false;
                break;
            case 2:
                fragment = getAccountFragment(child);
                ret = false;
                break;
            case 3:
                AccessState.instance().setUserLoggedOut(MainActivity.mContext);
                startActivity(new Intent(MainActivity.mContext, LoginActivity.class));
                getActivity().finish();
                break;
        }

        if(fragment != null) {
            FragmentManager fragmentManager = getActivity().getSupportFragmentManager();
            fragmentManager.beginTransaction()
                    .replace(R.id.container, fragment)
                    .commit();
        }
        return ret;
    }

    public Fragment getFeedFragment(int child){
        if(child != -1)
            return FeedFragment.newInstance(child);
        else
            return null;
    }

    public Fragment getAccountFragment(int child){
        switch(child){
            case 0:
                NavigationDrawerDialogs.showDeleteAccountDialog();
                break;
            case 1:
                NavigationDrawerDialogs.showChangePasswordDialog();
                break;
        }

        return null; // Change to AccountFragment if necessary
    }

    public boolean isDrawerOpen() {
        return mDrawerLayout != null && mDrawerLayout.isDrawerOpen(mFragmentContainerView);
    }

    public void setUp(int fragmentId, DrawerLayout drawerLayout) {
        mFragmentContainerView = getActivity().findViewById(fragmentId);
        mDrawerLayout = drawerLayout;

        mDrawerLayout.setDrawerShadow(R.drawable.drawer_shadow, GravityCompat.START);

        ActionBar actionBar = getActionBar();
        actionBar.setDisplayHomeAsUpEnabled(true);
        actionBar.setHomeButtonEnabled(true);

        mDrawerToggle = new ActionBarDrawerToggle(
                getActivity(),
                mDrawerLayout,
                R.drawable.ic_drawer,
                R.string.navigation_drawer_open,
                R.string.navigation_drawer_close
        ) {
            @Override
            public void onDrawerClosed(View drawerView) {
                super.onDrawerClosed(drawerView);
                if (!isAdded()) {
                    return;
                }
                getActivity().supportInvalidateOptionsMenu();
            }

            @Override
            public void onDrawerOpened(View drawerView) {
                super.onDrawerOpened(drawerView);
                if (!isAdded()) {
                    return;
                }
                if (!mUserLearnedDrawer) {
                    mUserLearnedDrawer = true;
                    SharedPreferences sp = PreferenceManager
                            .getDefaultSharedPreferences(getActivity());
                    sp.edit().putBoolean(PREF_USER_LEARNED_DRAWER, true).apply();
                }
                getActivity().supportInvalidateOptionsMenu(); // calls onPrepareOptionsMenu()
            }
        };

        if (!mUserLearnedDrawer && !mFromSavedInstanceState) {
            mDrawerLayout.openDrawer(mFragmentContainerView);
        }

        mDrawerLayout.post(new Runnable() {
            @Override
            public void run() {
                mDrawerToggle.syncState();
            }
        });
        mDrawerLayout.setDrawerListener(mDrawerToggle);
    }

    @Override
    public void onAttach(Activity activity) {
        super.onAttach(activity);
    }

    @Override
    public void onDetach() {
        super.onDetach();
    }

    @Override
    public void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        outState.putInt(STATE_SELECTED_GROUP, mCurrentSelectedGroup);
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        mDrawerToggle.onConfigurationChanged(newConfig);
    }

    @Override
    public void onCreateOptionsMenu(Menu menu, MenuInflater inflater) {
        if (mDrawerLayout != null && isDrawerOpen()) {
            inflater.inflate(R.menu.global, menu);
            showGlobalContextActionBar();
        }
        super.onCreateOptionsMenu(menu, inflater);
    }

    @Override
    public void onPrepareOptionsMenu(Menu menu){
        if(mDrawerLayout != null && isDrawerOpen()){
            menu.clear();
        }
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (mDrawerToggle.onOptionsItemSelected(item)) {
            return true;
        }
        select(2, 0);   // Jump to "Accounts"
        return super.onOptionsItemSelected(item);
    }

    private void showGlobalContextActionBar() {
        ActionBar actionBar = getActionBar();
        actionBar.setNavigationMode(ActionBar.NAVIGATION_MODE_STANDARD);
    }

    private ActionBar getActionBar() {
        ActionBar actionBar = ((ActionBarActivity) getActivity()).getSupportActionBar();
        actionBar.setTitle(getActivity().getTitle());
        return ((ActionBarActivity) getActivity()).getSupportActionBar();
    }

    public static class FeedFragment extends Fragment {

        private static final String ARG_SECTION_NUMBER = "section_number";
        private static final int GROUP_NUMBER = 1;
        private static int childSectionNumber;
        private static String title;
        private static ListView listView;

        public static FeedFragment newInstance(int sectionNumber) {
            FeedFragment fragment = new FeedFragment();
            Bundle args = new Bundle();
            args.putInt(ARG_SECTION_NUMBER, sectionNumber);
            fragment.setArguments(args);
            childSectionNumber = sectionNumber;
            return fragment;
        }

        public FeedFragment() {
        }

        @Override
        public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
            final View rootView = inflater.inflate(R.layout.fragment_main, container, false);
            listView = (ListView) rootView.findViewById(R.id.feed_list);

            listView.setOnItemClickListener(new AdapterView.OnItemClickListener() {
                @Override
                public void onItemClick(AdapterView<?> arg0, View arg1, int position, long arg3) {
                    Link link = (Link)listView.getItemAtPosition(position);
                    Intent intent = new Intent(getActivity(), BrowserActivity.class);
                    intent.putExtra("url", link.url);
                    startActivity(intent);
                }
            });

            title = (String)listAdapter.getChild(GROUP_NUMBER, childSectionNumber);
            FeedResults results = StorageManager.instance().readFeedResults(title, MainActivity.mContext);
            List sortedResults = sortLinks(results.userPageRank);
            ArrayAdapter<Link> adapter = new ArrayAdapter<>(listView.getContext(), R.layout.feed_item, sortedResults);
            listView.setAdapter(adapter);

            return rootView;
        }

        public List<Link> sortLinks(List<Link> results) {
            Collections.sort(results, new Comparator<Link>() {
                @Override
                public int compare(Link obj1, Link obj2) {
                    return (obj1.pageRank < obj2.pageRank) ? 1 : -1;
                }
            });
            if(results.get(0).pageRank == 0){
                Collections.reverse(results);
            }
            return results;
        }

        @Override
        public void onAttach(Activity activity) {
            super.onAttach(activity);
            ((MainActivity) activity).onSectionAttached(
                    getArguments().getInt(ARG_SECTION_NUMBER));
        }
    }

}
